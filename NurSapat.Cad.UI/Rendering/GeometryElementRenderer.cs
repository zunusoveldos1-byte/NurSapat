using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Rendering;

public static class GeometryElementRenderer
{
    private const double FallbackSpanMm = 2000;
    private const double MarkerRadiusPx = 8;

    /// <summary>Общий контур геометрии (рама, размеры, штапик-фолбэк) — единая тема цвета.</summary>
    private static readonly IBrush CadStrokeBrush = CadContourTheme.Brush;

    private static readonly IBrush DimensionColorBrush = CadContourTheme.Brush;
    private static readonly IBrush DimensionTextBrush = CadContourTheme.Brush;

    private static readonly IBrush FrameBrush = CadStrokeBrush;
    private static readonly IBrush GlazingFill = GlassRenderer.GlassFill;
    private static readonly IPen GlazingBeadPen = new Pen(CadContourTheme.Brush, 1);
    private static readonly IBrush AccentFill = new SolidColorBrush(Color.FromArgb(30, 0x6B, 0x72, 0x80));
    private static readonly IBrush AccentBrush = CadStrokeBrush;
    private static readonly IPen FramePen = new Pen(FrameBrush, 1.5);
    private static readonly IPen ThinPen = new Pen(FrameBrush, 1);
    private static readonly IPen AccentPen = ThinPen;

    private static readonly IPen DimensionPen = new Pen(
        DimensionColorBrush,
        1.2,
        lineCap: PenLineCap.Flat,
        lineJoin: PenLineJoin.Miter);
    private static readonly IPen DimensionTickPen = new Pen(
        DimensionColorBrush,
        1.5,
        lineCap: PenLineCap.Flat,
        lineJoin: PenLineJoin.Miter);

    private static readonly IPen DashedPen = new Pen(FrameBrush, 1.5, new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen DashedAccentPen = DashedPen;
    private static readonly IPen HatchPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 0x1F, 0x29, 0x37)), 0.8);

    private const double DimensionFrameGap = 0;
    private const double DimensionExtensionOffset = 21;
    private const double DimensionOverallOffset = 42;
    private const double DimensionBelowSillGap = 0;
    private const double DimensionSegmentOffsetBelowSill = 22;
    private const double DimensionOverallOffsetBelowSill = 46;
    private const double DimensionOvershootWorldMm = 1.5;
    private const double RadiusDimensionLabelOffsetPx = 10;
    private const double RadiusDimensionArcOffsetFactor = 0.65;
    public const double DimensionLabelFontSize = 11;
    private const double DimensionHitPadding = 6;
    private const double DimensionJointOverlapPx = 4;
    /// <summary>Смещение центра подписи от оси размерной линии (px), чтобы цифры не пересекались с линией.</summary>
    private const double DimensionLabelClearancePx = 9;
    private const double DimensionLabelBackdropPadX = 3;
    private const double DimensionLabelBackdropPadY = 1;
    private static readonly IBrush DimensionLabelBackdropBrush = Brushes.White;

    public static void DrawAll(DrawingContext context, CadEditorViewModel editor, IReadOnlyList<GeometryElement> elements)
    {
        if (editor == null)
        {
            return;
        }

        // Порядок: рама → переходной профиль → прочие несущие элементы.
        // Створки/остекление/фурнитура рисуются отдельными слоями после импостов.
        var snapshot = elements.Count == 0
            ? []
            : elements
                .OrderBy(GetStructuralDrawOrder)
                .ToList();
        foreach (var element in snapshot)
        {
            try
            {
                Draw(context, editor, element);
            }
            catch (Exception ex)
            {
                CadDiagnostics.LogWarning($"DrawAll failed for {element.Kind}", ex);
            }
        }
    }

    private static int GetStructuralDrawOrder(GeometryElement element) =>
        element.Kind switch
        {
            GeometryElementKind.DummyFrame or GeometryElementKind.ArchDummyFrame => 0,
            GeometryElementKind.Frame or GeometryElementKind.ArchFrame => 1,
            GeometryElementKind.TransitionFrame => 2,
            _ => 3
        };

    /// <summary>Слой остекления: после профиля створки, до линий открывания и фурнитуры.</summary>
    public static void DrawGlazingLayer(
        DrawingContext context,
        CadEditorViewModel editor,
        IEnumerable<GeometryElement> elements)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        var elementList = elements as IReadOnlyList<GeometryElement> ?? elements.ToList();

        foreach (var element in elementList)
        {
            if (element.Kind != GeometryElementKind.Glazing)
            {
                continue;
            }

            if (element.ParentBeadId is not null)
            {
                continue;
            }

            if (element.FillType is GlazingType.VerticalCorrugation or GlazingType.HorizontalCorrugation
                or GlazingType.SlantedCorrugation)
            {
                continue;
            }

            try
            {
                DrawGlazing(context, editor, element, elementList);
            }
            catch (Exception ex)
            {
                CadDiagnostics.LogWarning($"DrawGlazingLayer failed for {element.Id}", ex);
            }
        }

        foreach (var element in elementList)
        {
            if (element.Kind == GeometryElementKind.Glazing
                && element.FillType is GlazingType.VerticalCorrugation or GlazingType.HorizontalCorrugation
                    or GlazingType.SlantedCorrugation)
            {
                try
                {
                    DrawGlazing(context, editor, element, elementList);
                    CorrugationFillRenderer.DrawCorrugationElement(context, editor, element, elementList);
                }
                catch (Exception ex)
                {
                    CadDiagnostics.LogWarning($"DrawCorrugationGlazingLayer failed for {element.Id}", ex);
                }

                continue;
            }

            if (element.Kind == GeometryElementKind.Corrugation)
            {
                try
                {
                    CorrugationFillRenderer.DrawCorrugationElement(context, editor, element, elementList);
                }
                catch (Exception ex)
                {
                    CadDiagnostics.LogWarning($"DrawCorrugationLayer failed for {element.Id}", ex);
                }
            }
        }

        foreach (var element in elementList)
        {
            if (element.Kind == GeometryElementKind.MosquitoNet
                || (element.Kind == GeometryElementKind.Sash && element.HasMosquitoNet))
            {
                MosquitoNetRenderer.DrawMosquitoNetOverlay(context, editor, element, elementList);
            }
        }
    }

    public static void DrawPlacementPreview(
        DrawingContext context,
        CadEditorViewModel editor,
        NurSapat.Cad.Core.Models.WindowToolMode mode,
        CadRect bounds)
    {
        if (editor.Viewport == null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var element = CreatePreviewElement(mode, bounds);
        switch (mode)
        {
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrame:
                {
                    var (originX, originY, width, height) = GetWorldOriginBounds(element);
                    FrameVolumeRenderer.DrawFictitiousOutline(
                        context, editor, originX, originY, width, height, editor.FrameThickness);
                    break;
                }
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameTriangle:
                DrawTriangleFrame(context, editor, CreatePreviewElement(mode, bounds));
                break;
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameArch:
                DrawArchFrame(context, editor, CreatePreviewElement(mode, bounds), dashed: true);
                break;
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameCircle:
                DrawRoundDummyFrame(context, editor, CreatePreviewElement(mode, bounds));
                break;
        }
    }

    private static GeometryElement CreatePreviewElement(
        NurSapat.Cad.Core.Models.WindowToolMode mode,
        CadRect bounds)
    {
        var width = RoundPreview(bounds.Width);
        var height = mode switch
        {
            NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameArch => width / 2.0,
            NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameCircle => RoundPreview(bounds.Height),
            _ => RoundPreview(bounds.Height)
        };
        var originX = bounds.X;
        var originY = bounds.Y - bounds.Height;
        var kind = mode switch
        {
            NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameTriangle => GeometryElementKind.TriangleDummyFrame,
            NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameArch => GeometryElementKind.ArchDummyFrame,
            NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameCircle => GeometryElementKind.RoundDummyFrame,
            _ => GeometryElementKind.DummyFrame
        };

        return new GeometryElement
        {
            Kind = kind,
            X = (decimal)(originX + width / 2.0),
            Y = (decimal)(originY + height / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height,
            Shape = mode switch
            {
                NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameCircle => ConstructionShape.Circle,
                NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameTriangle => ConstructionShape.Triangle,
                _ => ConstructionShape.Rectangle
            }
        };
    }

    private static double ResolveTransitionProfileThickness(GeometryElement element) =>
        TransitionAdjacentFrameGeometry.ResolveTransitionProfileThickness(element);

    private static double RoundPreview(double value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static void Draw(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (element.WidthMm <= 0 || element.HeightMm <= 0)
        {
            CadDiagnostics.LogWarning($"Skip draw: invalid dimensions for {element.Kind}");
            return;
        }

        switch (element.Kind)
        {
            case GeometryElementKind.Mullion:
            case GeometryElementKind.EdgeMullion:
                DrawMullion(context, editor, element, dashed: false);
                break;
            case GeometryElementKind.DummyMullion:
                DrawMullion(context, editor, element, dashed: true);
                break;
            case GeometryElementKind.Frame:
                {
                    var (originX, originY, width, height) = GetWorldOriginBounds(element);
                    if (editor.FrameRoot != null &&
                        FrameMatchesElement(editor.FrameRoot, element) &&
                        editor.FrameRoot.Shape == ConstructionShape.Triangle)
                    {
                        ShapedFrameRenderer.DrawShapedFrame(
                            context,
                            editor,
                            originX,
                            originY,
                            width,
                            height,
                            editor.FrameThickness,
                            ConstructionShape.Triangle,
                            dashed: false);
                        if (editor.ShowDimensions)
                        {
                            DrawTriangleDimensions(context, editor, element);
                        }

                        break;
                    }

                    ProfileSegment? topProfile = null;
                    if (editor.FrameRoot != null &&
                        FrameMatchesElement(editor.FrameRoot, element))
                    {
                        editor.FrameRoot.EnsureTopProfile();
                        topProfile = editor.FrameRoot.TopProfile;
                    }

                    FrameVolumeRenderer.Draw(
                        context,
                        editor,
                        originX,
                        originY,
                        width,
                        height,
                        editor.FrameThickness,
                        dashed: false,
                        topProfile: topProfile);
                    if (editor.ShowDimensions)
                    {
                        DrawFrameDimensions(context, editor, element);
                    }
                    break;
                }
            case GeometryElementKind.TransitionFrame:
                {
                    var (originX, originY, width, height) = GetWorldOriginBounds(element);
                    var profileThickness = ResolveTransitionProfileThickness(element);

                    IReadOnlyDictionary<ImpostEdgeAlignment, double>? junctionAxes = null;
                    if (editor.FrameRoot is not null)
                    {
                        var centerX = (double)element.X;
                        var centerY = (double)element.Y;
                        var leaf = editor.FrameRoot.FindLeafAt(centerX, centerY);
                        if (leaf is not null)
                        {
                            junctionAxes = SashGeometry.ResolveExtremeImpostAxisLines(
                                leaf,
                                editor.FrameRoot,
                                editor.FrameThickness);
                        }
                    }

                    FrameVolumeRenderer.DrawTransitionProfileBand(
                        context,
                        editor,
                        originX,
                        originY,
                        width,
                        height,
                        profileThickness,
                        junctionAxes);
                    break;
                }
            case GeometryElementKind.DummyFrame:
                {
                    var (originX, originY, width, height) = GetWorldOriginBounds(element);
                    FrameVolumeRenderer.DrawFictitiousOutline(
                        context,
                        editor,
                        originX,
                        originY,
                        width,
                        height,
                        editor.FrameThickness);
                    if (editor.ShowDimensions)
                    {
                        DrawFrameDimensions(context, editor, element);
                    }

                    break;
                }
            case GeometryElementKind.ArchFrame:
                DrawArchFrame(context, editor, element);
                if (editor.ShowDimensions)
                {
                    DrawArchDimensions(context, editor, element);
                }

                break;
            case GeometryElementKind.ArchDummyFrame:
                DrawArchFrame(context, editor, element, dashed: true);
                if (editor.ShowDimensions)
                {
                    DrawArchDimensions(context, editor, element);
                }

                break;
            case GeometryElementKind.TriangleDummyFrame:
                DrawTriangleFrame(context, editor, element);
                if (editor.ShowDimensions)
                {
                    DrawTriangleDimensions(context, editor, element);
                }
                break;
            case GeometryElementKind.RoundDummyFrame:
                DrawRoundDummyFrame(context, editor, element);
                if (editor.ShowDimensions)
                {
                    DrawCircleDimensions(context, editor, element);
                }
                break;
            case GeometryElementKind.Sash:
                break;
            case GeometryElementKind.SlidingSash:
                DrawSlidingSash(context, editor, element);
                break;
            case GeometryElementKind.Glazing:
                break;
            case GeometryElementKind.DecorativeSpoke:
                break;
            case GeometryElementKind.Corrugation:
                break;
            case GeometryElementKind.MosquitoNet:
                break;
            case GeometryElementKind.Sill:
                break;
            case GeometryElementKind.Handle:
            case GeometryElementKind.Hardware:
                DrawPointMarker(context, editor, element);
                break;
            case GeometryElementKind.Bead:
                break;
            case GeometryElementKind.Accessory:
                DrawAccessory(context, editor, element);
                break;
            case GeometryElementKind.Installation:
                DrawInstallation(context, editor, element);
                break;
        }
    }

    private static void DrawMullion(DrawingContext context, CadEditorViewModel editor, GeometryElement element, bool dashed)
    {
        var point = new CadPoint((double)element.X, (double)element.Y);
        var (minX, maxX, minY, maxY) = ResolveFrameBounds(editor, point);
        var pen = dashed ? DashedPen : FramePen;

        var orientation = element.SourceToolId switch
        {
            "mullion-h" => 'h',
            "mullion-d" => 'd',
            _ => 'v'
        };

        if (orientation == 'd' && element.EndX.HasValue && element.EndY.HasValue)
        {
            DrawWorldLine(
                context,
                editor,
                pen,
                new CadPoint((double)element.X, (double)element.Y),
                new CadPoint((double)element.EndX.Value, (double)element.EndY.Value));
            return;
        }

        if (element.EndX.HasValue && element.EndY.HasValue)
        {
            DrawWorldLine(
                context,
                editor,
                pen,
                new CadPoint((double)element.X, (double)element.Y),
                new CadPoint((double)element.EndX.Value, (double)element.EndY.Value));
            return;
        }

        var (start, end) = orientation switch
        {
            'h' => (new CadPoint(minX, point.Y), new CadPoint(maxX, point.Y)),
            'd' => (new CadPoint(minX, minY), new CadPoint(maxX, maxY)),
            _ => (new CadPoint(point.X, minY), new CadPoint(point.X, maxY))
        };

        DrawWorldLine(context, editor, pen, start, end);
    }

    private static void DrawFrameDimensions(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (editor.FrameRoot != null &&
            FrameMatchesElement(editor.FrameRoot, element) &&
            editor.FrameRoot.TopProfile?.SegmentType == SegmentType.Arc)
        {
            DrawArchedTopFrameDimensions(context, editor, element, editor.FrameRoot.TopProfile);
            return;
        }

        if (editor.FrameRoot != null &&
            FrameMatchesElement(editor.FrameRoot, element) &&
            editor.FrameRoot.Shape == ConstructionShape.Triangle)
        {
            DrawTriangleDimensions(context, editor, element);
            return;
        }

        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var root = editor.FrameRoot;

        // Внешняя фиктивная коробка всегда строит размерные цепочки по FrameRoot:
        // строгий AABB-match может расходиться после сплитов/округления и оставляет только габариты.
        var isOuterDimensionHost = root is not null
            && (FrameMatchesElement(root, element)
                || (root.IsFictitiousContainer && DummyFrame.IsDummyKind(element.Kind)));

        if (isOuterDimensionHost)
        {
            try
            {
                if (TryDrawDeformedOuterDimensions(
                        context,
                        editor,
                        root!,
                        rect,
                        width,
                        height))
                {
                    return;
                }

                if (TryDrawProfileRailOuterDimensions(
                        context,
                        editor,
                        root!,
                        rect,
                        width,
                        height))
                {
                    return;
                }

                var hasBottomSegments = TryCollectBottomWidthSegments(
                    editor,
                    root!,
                    out var segmentWidths,
                    out var boundaryWorldX);
                var hasLeftSegments = TryCollectLeftHeightSegments(
                    editor,
                    root!,
                    out var segmentHeights,
                    out var boundaryWorldY);

                if (hasBottomSegments || hasLeftSegments)
                {
                    DrawScreenDimensionsWithSegments(
                        context,
                        editor,
                        rect,
                        element,
                        hasBottomSegments ? segmentWidths : null,
                        hasBottomSegments ? boundaryWorldX : null,
                        hasLeftSegments ? segmentHeights : null,
                        hasLeftSegments ? boundaryWorldY : null,
                        width,
                        height,
                        registerHits: true);
                    return;
                }
            }
            catch (Exception ex)
            {
                CadDiagnostics.LogWarning(
                    "DrawFrameDimensions: segmented chain failed; falling back to overall dimensions",
                    ex);
            }

            // Габариты 2300×2200 и т.п. должны оставаться даже если сегменты
            // (1300/900) временно не собрались после установки профилей.
            DrawScreenDimensions(context, rect, $"{width:0}", $"{height:0}", editor, registerHits: true);
            return;
        }
        else if (root is not null
                 && element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                     or GeometryElementKind.DummyFrame
                 && TryCollectSmartWidthSegmentsForNestedFrame(
                     editor,
                     root,
                     element,
                     out var nestedWidths,
                     out var nestedBoundariesX,
                     out var nestedHeights,
                     out var nestedBoundariesY))
        {
            DrawScreenDimensionsWithSegments(
                context,
                editor,
                rect,
                element,
                nestedWidths.Count > 0 ? nestedWidths : null,
                nestedBoundariesX.Count > 0 ? nestedBoundariesX : null,
                nestedHeights.Count > 0 ? nestedHeights : null,
                nestedBoundariesY.Count > 0 ? nestedBoundariesY : null,
                width,
                height,
                registerHits: true);
            return;
        }

        DrawScreenDimensions(context, rect, $"{width:0}", $"{height:0}", editor, registerHits: true);
    }

    private static void DrawArchDimensions(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var segment = ProfileSegment.CreateTopRail(0, 0, width, height);
        segment.SegmentType = SegmentType.Arc;
        segment.ArcHeight = height;
        DrawArchedTopFrameDimensions(context, editor, element, segment, width, height);
    }

    private static void DrawArchedTopFrameDimensions(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement element,
        ProfileSegment topProfile,
        double? boxWidthOverride = null,
        double? boxHeightOverride = null)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        var (originX, originY, width, boxHeight) = GetWorldOriginBounds(element);
        width = boxWidthOverride ?? width;
        boxHeight = boxHeightOverride ?? boxHeight;

        var archHeight = topProfile.ArcHeight;
        var totalHeight = boxHeight + archHeight;
        var chordWidth = topProfile.ChordWidth;
        var outerRadius = ProfileSegment.ComputeRadiusFromSagitta(chordWidth, archHeight);
        var springingY = originY + boxHeight;
        var apexY = springingY + archHeight;
        var center = topProfile.GetArcCenterWorld();

        const double frameGap = DimensionFrameGap;
        const double dimOffset = DimensionExtensionOffset;
        const double parallelGap = 24;

        var screenBottomLeft = ToScreenPoint(editor, originX, originY);
        var screenSpringingLeft = ToScreenPoint(editor, originX, springingY);
        var screenApexCenter = ToScreenPoint(editor, originX + width / 2.0, apexY);
        var screenBottomRight = ToScreenPoint(editor, originX + width, originY);

        var xDimInner = screenBottomLeft.X - dimOffset;
        var xDimOuter = xDimInner - parallelGap;

        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            screenBottomLeft.X,
            screenBottomLeft.Y,
            xDimInner);
        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            screenBottomLeft.X,
            screenSpringingLeft.Y,
            xDimInner);
        DrawVerticalDimensionSegment(context, editor, xDimInner, screenBottomLeft.Y, screenSpringingLeft.Y);

        var rectHeightCenter = new Point(xDimInner, (screenBottomLeft.Y + screenSpringingLeft.Y) / 2);
        var rectHeightLabel = $"{boxHeight:0}";
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallHeight))
        {
            DrawVerticalTextAlongSide(context, rectHeightLabel, rectHeightCenter, DimensionTextBrush);
        }

        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            screenBottomLeft.X,
            screenBottomLeft.Y,
            xDimOuter);
        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            screenApexCenter.X,
            screenApexCenter.Y,
            xDimOuter);
        DrawVerticalDimensionSegment(context, editor, xDimOuter, screenBottomLeft.Y, screenApexCenter.Y);

        var totalHeightCenter = new Point(xDimOuter, (screenBottomLeft.Y + screenApexCenter.Y) / 2);
        var totalHeightLabel = $"{totalHeight:0}";
        DrawVerticalTextAlongSide(context, totalHeightLabel, totalHeightCenter, DimensionTextBrush);

        var yExtStart = screenBottomLeft.Y + frameGap;
        var yDim = screenBottomLeft.Y + dimOffset;
        DrawVerticalExtensionToHorizontalDimension(
            context,
            editor,
            screenBottomLeft.X,
            yExtStart,
            yDim);
        DrawVerticalExtensionToHorizontalDimension(
            context,
            editor,
            screenBottomRight.X,
            yExtStart,
            yDim);
        DrawHorizontalDimensionSegment(context, editor, yDim, screenBottomLeft.X, screenBottomRight.X);
        DrawCenteredText(context, $"{width:0}", new Point((screenBottomLeft.X + screenBottomRight.X) / 2, yDim + DimensionLabelClearancePx), DimensionTextBrush);

        var radialEndWorld = ResolveOffsetRadiusDimensionEndPoint(topProfile, outerRadius);
        var centerScreen = ToScreenPoint(editor, center.X, center.Y);
        var radialEndScreen = ToScreenPoint(editor, radialEndWorld.X, radialEndWorld.Y);
        var radiusPen = editor.CurrentToolMode == WindowToolMode.RadiusVertex
            ? FrameVolumeRenderer.SelectionDimensionPen
            : DimensionPen;
        var radiusLabelBrush = editor.CurrentToolMode == WindowToolMode.RadiusVertex
            ? FrameVolumeRenderer.SelectionHighlightBrush
            : DimensionTextBrush;
        context.DrawLine(radiusPen, centerScreen, radialEndScreen);
        var radiusLabelPoint = new Point(
            (centerScreen.X + radialEndScreen.X) / 2,
            (centerScreen.Y + radialEndScreen.Y) / 2);
        DrawTextAlongLine(
            context,
            $"R {outerRadius:0}",
            radiusLabelPoint,
            centerScreen,
            radialEndScreen,
            radiusLabelBrush,
            RadiusDimensionLabelOffsetPx);
    }

    /// <summary>
    /// Конец размерной линии радиуса: ~65% от вершины дуги к правому краю, чтобы не накладываться на центральный импост.
    /// </summary>
    private static CadPoint ResolveOffsetRadiusDimensionEndPoint(ProfileSegment topProfile, double outerRadius)
    {
        var center = topProfile.GetArcCenterWorld();
        var (startAngle, endAngle) = topProfile.GetOuterArcAnglesRad();
        var midAngle = FrameProfileGeometry.ComputeArcMidAngleRad(startAngle, endAngle);
        var halfSpan = ProfileSegment.PositiveAngleDelta(endAngle, startAngle) / 2.0;
        var dimAngle = FrameProfileGeometry.NormalizeAngleRad(
            midAngle - RadiusDimensionArcOffsetFactor * halfSpan);
        return CircularArc.PointAtAngle(center, outerRadius, dimAngle);
    }

    private static void DrawTriangleDimensions(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var bl = new CadPoint(originX, originY);
        var br = new CadPoint(originX + width, originY);
        var top = new CadPoint(originX + width / 2.0, originY + height);

        var blScreen = ToScreenPoint(editor, bl.X, bl.Y);
        var brScreen = ToScreenPoint(editor, br.X, br.Y);
        var topScreen = ToScreenPoint(editor, top.X, top.Y);

        var widthMm = (double)element.WidthMm;
        var heightMm = (double)element.HeightMm;
        var half = widthMm / 2.0;

        const double frameGap = DimensionFrameGap;
        const double segOffset = DimensionExtensionOffset;
        const double overallOffset = DimensionOverallOffset;

        var xDim = blScreen.X - segOffset;
        DrawHorizontalExtensionToVerticalDimension(context, editor, blScreen.X, topScreen.Y, xDim);
        DrawHorizontalExtensionToVerticalDimension(context, editor, blScreen.X, blScreen.Y, xDim);
        DrawVerticalDimensionSegment(context, editor, xDim, topScreen.Y, blScreen.Y);
        DrawVerticalTextAlongSide(context, $"{heightMm:0}", new Point(xDim, (topScreen.Y + blScreen.Y) / 2), DimensionTextBrush);

        var yExtStart = blScreen.Y + frameGap;
        var ySeg = blScreen.Y + segOffset;
        DrawVerticalExtensionToHorizontalDimension(context, editor, blScreen.X, yExtStart, ySeg);
        DrawVerticalExtensionToHorizontalDimension(context, editor, topScreen.X, yExtStart, ySeg);
        DrawVerticalExtensionToHorizontalDimension(context, editor, brScreen.X, yExtStart, ySeg);
        DrawHorizontalDimensionSegment(context, editor, ySeg, blScreen.X, topScreen.X);
        DrawHorizontalDimensionSegment(context, editor, ySeg, topScreen.X, brScreen.X);

        DrawCenteredText(context, $"{half:0}", new Point((blScreen.X + topScreen.X) / 2, ySeg - DimensionLabelClearancePx), DimensionTextBrush);
        DrawCenteredText(context, $"{half:0}", new Point((topScreen.X + brScreen.X) / 2, ySeg - DimensionLabelClearancePx), DimensionTextBrush);

        var yOverall = blScreen.Y + overallOffset;
        DrawVerticalExtensionToHorizontalDimension(context, editor, blScreen.X, yExtStart, yOverall);
        DrawVerticalExtensionToHorizontalDimension(context, editor, brScreen.X, yExtStart, yOverall);
        DrawHorizontalDimensionSegment(context, editor, yOverall, blScreen.X, brScreen.X);

        DrawCenteredText(context, $"{widthMm:0}", new Point((blScreen.X + brScreen.X) / 2, yOverall + DimensionLabelClearancePx), DimensionTextBrush);
    }

    private static void DrawCircleDimensions(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var diameter = (double)element.WidthMm;
        var radius = diameter / 2;
        DrawScreenDimensions(context, rect, $"{diameter:0}", $"{diameter:0}");

        var center = rect.Center;
        var topLeft = new Point(rect.Left, rect.Top);
        var bottomRight = new Point(rect.Right, rect.Bottom);
        context.DrawLine(DimensionPen, topLeft, bottomRight);
        DrawCenteredText(context, $"{radius:0}", new Point((center.X + topLeft.X) / 2, (center.Y + topLeft.Y) / 2), DimensionTextBrush);
        DrawCenteredText(context, $"{radius:0}", new Point((center.X + bottomRight.X) / 2, (center.Y + bottomRight.Y) / 2), DimensionTextBrush);
    }

    public static void DrawScreenDimensions(
        DrawingContext context,
        Rect rect,
        string widthLabel,
        string heightLabel,
        CadEditorViewModel? editor = null,
        bool registerHits = false)
    {
        var bottomAnchor = ResolveBottomDimensionAnchorScreenY(editor, rect.Bottom);
        var frameGap = ResolveBottomDimensionGap(editor);
        var (_, overallOffset) = ResolveBottomDimensionOffsets(editor);
        var dimOffset = HasAttachedSill(editor) ? overallOffset : DimensionExtensionOffset;

        var yExtStart = bottomAnchor + frameGap;
        var yDim = bottomAnchor + dimOffset;
        DrawVerticalExtensionToHorizontalDimension(
            context,
            editor,
            rect.Left,
            yExtStart,
            yDim);
        DrawVerticalExtensionToHorizontalDimension(
            context,
            editor,
            rect.Right,
            yExtStart,
            yDim);
        DrawHorizontalDimensionSegment(context, editor, yDim, rect.Left, rect.Right);

        var widthCenter = new Point(rect.Center.X, yDim + DimensionLabelClearancePx);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallWidth))
        {
            DrawCenteredText(context, widthLabel, widthCenter, DimensionTextBrush);
        }

        if (registerHits && editor != null && TryParseDimensionValue(widthLabel, out var widthMm))
        {
            var (textBounds, hitRect) = MeasureHorizontalTextBounds(widthLabel, widthCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallWidth,
                ValueMm = widthMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = false
            });
        }

        var xDim = rect.Left - dimOffset;
        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            rect.Left,
            rect.Top,
            xDim);
        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            rect.Left,
            rect.Bottom,
            xDim);
        DrawVerticalDimensionSegment(context, editor, xDim, rect.Top, rect.Bottom);

        var heightCenter = new Point(xDim, rect.Center.Y);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallHeight))
        {
            DrawVerticalTextAlongSide(context, heightLabel, heightCenter, DimensionTextBrush);
        }

        if (registerHits && editor != null && TryParseDimensionValue(heightLabel, out var heightMm))
        {
            var (textBounds, hitRect) = MeasureVerticalTextBounds(heightLabel, heightCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallHeight,
                ValueMm = heightMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = true
            });
        }
    }

    private static void DrawScreenDimensionsWithSegments(
        DrawingContext context,
        CadEditorViewModel editor,
        Rect rect,
        GeometryElement element,
        IReadOnlyList<double>? segmentWidthsMm,
        IReadOnlyList<double>? boundaryWorldX,
        IReadOnlyList<double>? segmentHeightsMm,
        IReadOnlyList<double>? boundaryWorldY,
        double totalWidthMm,
        double heightMm,
        bool registerHits = false)
    {
        var (_, originY, _, _) = GetWorldOriginBounds(element);

        // Один проём по оси = дубликат габарита; детальную цепочку рисуем только
        // при наличии внутренних делений (импостов / нескольких секций).
        if (segmentHeightsMm is { Count: > 1 } && boundaryWorldY is { Count: >= 3 })
        {
            var screenBoundariesY = boundaryWorldY
                .Select(worldY => WorldYToScreenY(editor, worldY, editor.FrameRoot!.X))
                .ToList();
            DrawLeftHeightDimensionChain(
                context,
                editor,
                rect,
                screenBoundariesY,
                boundaryWorldY,
                segmentHeightsMm,
                heightMm,
                registerHits);
        }
        else
        {
            DrawLeftHeightDimension(context, rect, heightMm, editor, registerHits);
        }

        if (segmentWidthsMm is { Count: > 1 } && boundaryWorldX is { Count: >= 3 })
        {
            var screenBoundariesX = boundaryWorldX
                .Select(worldX => WorldXToScreenX(editor, worldX, originY))
                .ToList();
            DrawBottomWidthDimensionChain(
                context,
                editor,
                screenBoundariesX,
                boundaryWorldX,
                segmentWidthsMm,
                totalWidthMm,
                ResolveBottomDimensionAnchorScreenY(editor, rect.Bottom),
                registerHits);
        }
        else
        {
            DrawBottomOverallWidthDimension(
                context,
                editor,
                rect,
                totalWidthMm,
                registerHits);
        }
    }

    private static bool HasAttachedSill(CadEditorViewModel? editor) =>
        editor?.FrameRoot?.GetConstructionRoot()?.HasSill == true;

    private static double ResolveBottomDimensionAnchorScreenY(CadEditorViewModel? editor, double frameBottomScreenY)
    {
        if (!HasAttachedSill(editor) || editor?.Viewport is null || editor.FrameRoot is null)
        {
            return frameBottomScreenY;
        }

        var root = editor.FrameRoot.GetConstructionRoot();
        var thickness = root.AttachedSill?.ThicknessMm > 0
            ? root.AttachedSill.ThicknessMm
            : SillElement.StandardThicknessMm;
        var sillBottomWorldY = root.Y - thickness;
        return ToScreenPoint(editor, root.X, sillBottomWorldY).Y;
    }

    private static (double segmentOffset, double overallOffset) ResolveBottomDimensionOffsets(CadEditorViewModel? editor) =>
        HasAttachedSill(editor)
            ? (DimensionSegmentOffsetBelowSill, DimensionOverallOffsetBelowSill)
            : (DimensionExtensionOffset, DimensionOverallOffset);

    private static double ResolveBottomDimensionGap(CadEditorViewModel? editor) =>
        HasAttachedSill(editor) ? DimensionBelowSillGap : DimensionFrameGap;

    private static void DrawRightSideHeightDimension(
        DrawingContext context,
        Rect rect,
        double heightMm,
        double bottomWorldY,
        CadEditorViewModel editor,
        bool registerHits)
    {
        var topRight = editor.FrameRoot!.GetOuterCorner(FrameOuterCornerKind.TopRight);
        var bottomRight = editor.FrameRoot.GetOuterCorner(FrameOuterCornerKind.BottomRight);
        var topObjectX = WorldXToScreenX(editor, topRight.X, topRight.Y);
        var bottomObjectX = WorldXToScreenX(editor, bottomRight.X, bottomRight.Y);
        var topY = WorldYToScreenY(editor, topRight.Y, topRight.X);
        var bottomY = WorldYToScreenY(editor, bottomRight.Y, bottomRight.X);
        var xDim = rect.Right + DimensionExtensionOffset;

        DrawHorizontalExtensionToVerticalDimension(context, editor, topObjectX, topY, xDim);
        DrawHorizontalExtensionToVerticalDimension(context, editor, bottomObjectX, bottomY, xDim);
        DrawVerticalDimensionSegment(context, editor, xDim, topY, bottomY);

        var label = $"{heightMm:0}";
        var center = new Point(xDim, (topY + bottomY) / 2.0);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.SideHeight))
        {
            DrawVerticalTextAlongSide(context, label, center, DimensionTextBrush);
        }

        if (registerHits)
        {
            var (textBounds, hitRect) = MeasureVerticalTextBounds(label, center);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.SideHeight,
                ValueMm = heightMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = true,
                VertexCorner = FrameOuterCornerKind.TopRight,
                BaseWorldY = bottomWorldY
            });
        }
    }

    private static void DrawRightHeightDimensionChain(
        DrawingContext context,
        CadEditorViewModel editor,
        Rect rect,
        IReadOnlyList<double> screenBoundaries,
        IReadOnlyList<double> objectScreenX,
        IReadOnlyList<double> boundaryWorldY,
        IReadOnlyList<double> segmentHeightsMm,
        double totalHeightMm,
        bool registerHits)
    {
        var xSeg = rect.Right + DimensionExtensionOffset;
        var xOverall = rect.Right + DimensionOverallOffset;

        for (var i = 0; i < screenBoundaries.Count; i++)
        {
            DrawHorizontalExtensionToVerticalDimension(
                context,
                editor,
                objectScreenX[i],
                screenBoundaries[i],
                xSeg);
        }

        for (var i = 0; i < segmentHeightsMm.Count; i++)
        {
            var y1 = screenBoundaries[i];
            var y2 = screenBoundaries[i + 1];
            DrawVerticalDimensionSegment(context, editor, xSeg, y1, y2);

            var label = $"{segmentHeightsMm[i]:0}";
            var center = new Point(xSeg, (y1 + y2) / 2);
            if (!IsDimensionLabelHidden(editor, DimensionHitKind.RightSegmentHeight, i))
            {
                DrawVerticalTextAlongSide(context, label, center, DimensionTextBrush);
            }

            if (registerHits)
            {
                var (textBounds, hitRect) = MeasureVerticalTextBounds(label, center);
                editor.RegisterDimensionHit(new DimensionHitInfo
                {
                    Kind = DimensionHitKind.RightSegmentHeight,
                    SegmentIndex = i,
                    ValueMm = segmentHeightsMm[i],
                    TextBounds = textBounds,
                    ScreenRect = hitRect,
                    IsVertical = true,
                    BoundaryWorldY = boundaryWorldY,
                    VertexCorner = FrameOuterCornerKind.TopRight,
                    MoveUpperBoundary = i == 0
                });
            }
        }

        var top = screenBoundaries[^1];
        var bottom = screenBoundaries[0];
        var hasProjectedApex = Math.Abs(
            boundaryWorldY[^1]
            - editor.FrameRoot!.GetOuterCorner(FrameOuterCornerKind.TopRight).Y)
            > CadMath.MmPrecision;
        if (!hasProjectedApex)
        {
            DrawHorizontalExtensionToVerticalDimension(
                context,
                editor,
                objectScreenX[^1],
                top,
                xOverall);
        }

        DrawHorizontalExtensionToVerticalDimension(
            context,
            editor,
            objectScreenX[0],
            bottom,
            xOverall);
        DrawVerticalDimensionSegment(context, editor, xOverall, top, bottom);

        var overallLabel = $"{totalHeightMm:0}";
        var overallCenter = new Point(xOverall, (top + bottom) / 2);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallHeight))
        {
            DrawVerticalTextAlongSide(context, overallLabel, overallCenter, DimensionTextBrush);
        }

        if (registerHits)
        {
            var (textBounds, hitRect) = MeasureVerticalTextBounds(overallLabel, overallCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallHeight,
                ValueMm = totalHeightMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = true
            });
        }
    }

    private static void DrawLeftHeightDimension(
        DrawingContext context,
        Rect rect,
        double heightMm,
        CadEditorViewModel? editor = null,
        bool registerHits = false)
    {
        const double dimOffset = DimensionExtensionOffset;
        var xDim = rect.Left - dimOffset;
        DrawHorizontalExtensionToVerticalDimension(context, editor, rect.Left, rect.Top, xDim);
        DrawHorizontalExtensionToVerticalDimension(context, editor, rect.Left, rect.Bottom, xDim);
        DrawVerticalDimensionSegment(context, editor, xDim, rect.Top, rect.Bottom);

        var label = $"{heightMm:0}";
        var heightCenter = new Point(xDim, rect.Center.Y);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallHeight))
        {
            DrawVerticalTextAlongSide(context, label, heightCenter, DimensionTextBrush);
        }

        if (registerHits && editor != null)
        {
            var (textBounds, hitRect) = MeasureVerticalTextBounds(label, heightCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallHeight,
                ValueMm = heightMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = true
            });
        }
    }

    private static void DrawLeftHeightDimensionChain(
        DrawingContext context,
        CadEditorViewModel? editor,
        Rect rect,
        IReadOnlyList<double> screenBoundaries,
        IReadOnlyList<double> boundaryWorldY,
        IReadOnlyList<double> segmentHeightsMm,
        double totalHeightMm,
        bool registerHits = false,
        IReadOnlyList<double>? objectScreenX = null)
    {
        const double frameGap = DimensionFrameGap;
        const double segOffset = DimensionExtensionOffset;
        const double overallOffset = DimensionOverallOffset;

        var xExtStart = rect.Left - frameGap;
        var xSeg = rect.Left - segOffset;
        var xOverall = rect.Left - overallOffset;

        for (var boundaryIndex = 0; boundaryIndex < screenBoundaries.Count; boundaryIndex++)
        {
            var startX = objectScreenX is { Count: > 0 }
                         && boundaryIndex < objectScreenX.Count
                ? objectScreenX[boundaryIndex]
                : xExtStart;
            var isOuterBoundary = boundaryIndex == 0
                                  || boundaryIndex == screenBoundaries.Count - 1;
            DrawHorizontalExtensionToVerticalDimension(
                context,
                editor,
                startX,
                screenBoundaries[boundaryIndex],
                isOuterBoundary ? xOverall : xSeg);
        }

        var impostCount = boundaryWorldY.Count - 2;
        for (var i = 0; i < segmentHeightsMm.Count; i++)
        {
            var y1 = screenBoundaries[i];
            var y2 = screenBoundaries[i + 1];
            DrawVerticalDimensionSegment(context, editor, xSeg, y1, y2);

            var label = $"{segmentHeightsMm[i]:0}";
            var center = new Point(xSeg, (y1 + y2) / 2);
            if (!IsDimensionLabelHidden(editor, DimensionHitKind.SegmentHeight, i))
            {
                DrawVerticalTextAlongSide(context, label, center, DimensionTextBrush);
            }

            if (registerHits && editor?.FrameRoot != null)
            {
                var moveUpper = i < impostCount;
                var impostTarget = TryResolveHorizontalImpostForSegment(editor.FrameRoot, boundaryWorldY, i);
                FrameOuterCornerKind? vertexCorner = null;
                var topLeftY = editor.FrameRoot.GetOuterCorner(FrameOuterCornerKind.TopLeft).Y;
                if (impostTarget is null)
                {
                    if (Math.Abs(boundaryWorldY[i + 1] - topLeftY) <= CadMath.MmPrecision)
                    {
                        vertexCorner = FrameOuterCornerKind.TopLeft;
                        moveUpper = true;
                    }
                    else if (Math.Abs(boundaryWorldY[i] - topLeftY) <= CadMath.MmPrecision)
                    {
                        vertexCorner = FrameOuterCornerKind.TopLeft;
                        moveUpper = false;
                    }
                }
                var (textBounds, hitRect) = MeasureVerticalTextBounds(label, center);
                editor.RegisterDimensionHit(new DimensionHitInfo
                {
                    Kind = DimensionHitKind.SegmentHeight,
                    SegmentIndex = i,
                    ValueMm = segmentHeightsMm[i],
                    TextBounds = textBounds,
                    ScreenRect = hitRect,
                    IsVertical = true,
                    BoundaryWorldY = boundaryWorldY,
                    ImpostTarget = impostTarget,
                    VertexCorner = vertexCorner,
                    MoveUpperBoundary = moveUpper
                });
            }
        }

        var top = screenBoundaries[^1];
        var bottom = screenBoundaries[0];
        DrawVerticalDimensionSegment(context, editor, xOverall, top, bottom);

        var overallLabel = $"{totalHeightMm:0}";
        var overallCenter = new Point(xOverall, (top + bottom) / 2);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallHeight))
        {
            DrawVerticalTextAlongSide(context, overallLabel, overallCenter, DimensionTextBrush);
        }

        if (registerHits && editor != null)
        {
            var (textBounds, hitRect) = MeasureVerticalTextBounds(overallLabel, overallCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallHeight,
                ValueMm = totalHeightMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = true
            });
        }
    }

    private static void DrawBottomOverallWidthDimension(
        DrawingContext context,
        CadEditorViewModel? editor,
        Rect rect,
        double totalWidthMm,
        bool registerHits = false)
    {
        var bottomAnchor = ResolveBottomDimensionAnchorScreenY(editor, rect.Bottom);
        var frameGap = ResolveBottomDimensionGap(editor);
        var (_, overallOffset) = ResolveBottomDimensionOffsets(editor);
        var dimOffset = HasAttachedSill(editor) ? overallOffset : DimensionExtensionOffset;

        var yExtStart = bottomAnchor + frameGap;
        var yDim = bottomAnchor + dimOffset;
        DrawVerticalExtensionToHorizontalDimension(context, editor, rect.Left, yExtStart, yDim);
        DrawVerticalExtensionToHorizontalDimension(context, editor, rect.Right, yExtStart, yDim);
        DrawHorizontalDimensionSegment(context, editor, yDim, rect.Left, rect.Right);

        var overallLabel = $"{totalWidthMm:0}";
        var overallCenter = new Point(rect.Center.X, yDim + DimensionLabelClearancePx);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallWidth))
        {
            DrawCenteredText(context, overallLabel, overallCenter, DimensionTextBrush);
        }

        if (registerHits && editor != null)
        {
            var (textBounds, hitRect) = MeasureHorizontalTextBounds(overallLabel, overallCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallWidth,
                ValueMm = totalWidthMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = false
            });
        }
    }

    private static void DrawBottomWidthDimensionChain(
        DrawingContext context,
        CadEditorViewModel? editor,
        IReadOnlyList<double> screenBoundaries,
        IReadOnlyList<double> boundaryWorldX,
        IReadOnlyList<double> segmentWidthsMm,
        double totalWidthMm,
        double outerBottomScreenY,
        bool registerHits = false,
        FrameNode? profileRailRoot = null,
        FrameProfileRail? profileRail = null)
    {
        var frameGap = ResolveBottomDimensionGap(editor);
        var (segOffset, overallOffset) = ResolveBottomDimensionOffsets(editor);

        var yExtStart = outerBottomScreenY + frameGap;
        var ySeg = outerBottomScreenY + segOffset;

        foreach (var boundaryX in screenBoundaries)
        {
            DrawVerticalExtensionToHorizontalDimension(context, editor, boundaryX, yExtStart, ySeg);
        }

        var impostCount = boundaryWorldX.Count - 2;
        for (var i = 0; i < segmentWidthsMm.Count; i++)
        {
            var x1 = screenBoundaries[i];
            var x2 = screenBoundaries[i + 1];
            DrawHorizontalDimensionSegment(context, editor, ySeg, x1, x2);

            var label = $"{segmentWidthsMm[i]:0}";
            var center = new Point((x1 + x2) / 2, ySeg - DimensionLabelClearancePx);
            var hitKind = profileRail is null
                ? DimensionHitKind.SegmentWidth
                : DimensionHitKind.ProfileRailSegment;
            if (!IsDimensionLabelHidden(editor, hitKind, i))
            {
                DrawCenteredText(context, label, center, DimensionTextBrush);
            }

            if (registerHits && editor?.FrameRoot != null)
            {
                if (profileRailRoot is not null && profileRail is FrameProfileRail rail)
                {
                    var joints = profileRailRoot.GetProfileRailJoints(rail);
                    if (joints.Count > 0)
                    {
                        var moveRightJoint = i < joints.Count;
                        var joint = moveRightJoint ? joints[i] : joints[^1];
                        var (profileTextBounds, profileHitRect) = MeasureHorizontalTextBounds(label, center);
                        editor.RegisterDimensionHit(new DimensionHitInfo
                        {
                            Kind = DimensionHitKind.ProfileRailSegment,
                            ValueMm = segmentWidthsMm[i],
                            TextBounds = profileTextBounds,
                            ScreenRect = profileHitRect,
                            SegmentIndex = i,
                            ProfileRail = rail,
                            RailJointId = joint.Id,
                            MoveRightBoundary = moveRightJoint
                        });
                    }

                    continue;
                }

                var moveRight = i < impostCount;
                var impostTarget = TryResolveVerticalImpostForSegment(editor.FrameRoot, boundaryWorldX, i);
                var (textBounds, hitRect) = MeasureHorizontalTextBounds(label, center);
                editor.RegisterDimensionHit(new DimensionHitInfo
                {
                    Kind = DimensionHitKind.SegmentWidth,
                    SegmentIndex = i,
                    ValueMm = segmentWidthsMm[i],
                    TextBounds = textBounds,
                    ScreenRect = hitRect,
                    IsVertical = false,
                    BoundaryWorldX = boundaryWorldX,
                    ImpostTarget = impostTarget,
                    MoveRightBoundary = moveRight
                });
            }
        }

        var yOverall = outerBottomScreenY + overallOffset;
        var left = screenBoundaries[0];
        var right = screenBoundaries[^1];
        DrawVerticalExtensionToHorizontalDimension(context, editor, left, yExtStart, yOverall);
        DrawVerticalExtensionToHorizontalDimension(context, editor, right, yExtStart, yOverall);
        DrawHorizontalDimensionSegment(context, editor, yOverall, left, right);

        var overallLabel = $"{totalWidthMm:0}";
        var overallCenter = new Point((left + right) / 2, yOverall + DimensionLabelClearancePx);
        if (!IsDimensionLabelHidden(editor, DimensionHitKind.OverallWidth))
        {
            DrawCenteredText(context, overallLabel, overallCenter, DimensionTextBrush);
        }

        if (registerHits && editor != null)
        {
            var (textBounds, hitRect) = MeasureHorizontalTextBounds(overallLabel, overallCenter);
            editor.RegisterDimensionHit(new DimensionHitInfo
            {
                Kind = DimensionHitKind.OverallWidth,
                ValueMm = totalWidthMm,
                TextBounds = textBounds,
                ScreenRect = hitRect,
                IsVertical = false
            });
        }
    }

    /// <summary>
    /// Размеры по внешним стыкам реек профиля.
    /// Цепочки split-рейки и импостов независимы: разбиение верха не сбрасывает низ по импостам.
    /// </summary>
    private static bool TryDrawDeformedOuterDimensions(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode root,
        Rect rect,
        double totalWidthMm,
        double totalHeightMm)
    {
        if (!root.HasDeformedOuterContour()
            && !root.ProfileRailJoints.Any(joint => Math.Abs(joint.NormalOffsetMm) > CadMath.Epsilon))
        {
            return false;
        }

        var corners = root.GetOuterCorners();
        var contour = root.GetOuterContourPoints();
        var minX = contour.Min(point => point.X);
        var maxX = contour.Max(point => point.X);
        var minY = contour.Min(point => point.Y);
        var maxY = contour.Max(point => point.Y);
        var screenTopLeft = editor.Viewport.WorldToScreen(new CadPoint(minX, maxY));
        var screenBottomRight = editor.Viewport.WorldToScreen(new CadPoint(maxX, minY));
        rect = new Rect(
            new Point(screenTopLeft.X, screenTopLeft.Y),
            new Point(screenBottomRight.X, screenBottomRight.Y));
        totalWidthMm = CadMath.RoundMm(maxX - minX);
        totalHeightMm = CadMath.RoundMm(maxY - minY);

        // Высота слева: уникальные Y углов левой стороны + габарит AABB.
        var leftYs = new SortedSet<double>(CadMathComparer.Instance)
        {
            CadMath.RoundMm(corners.BottomLeft.Y),
            CadMath.RoundMm(corners.TopLeft.Y),
            CadMath.RoundMm(root.Y),
            CadMath.RoundMm(root.Y + root.Height)
        };

        if (root.HasProfileRailJoints(FrameProfileRail.Left))
        {
            foreach (var y in root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Left))
            {
                leftYs.Add(CadMath.RoundMm(y));
            }
        }

        var leftBounds = leftYs.ToList();
        if (leftBounds.Count >= 2)
        {
            var leftLengths = new List<double>();
            for (var i = 0; i < leftBounds.Count - 1; i++)
            {
                leftLengths.Add(CadMath.RoundMm(leftBounds[i + 1] - leftBounds[i]));
            }

            if (leftLengths.Count > 1)
            {
                var screenY = leftBounds
                    .Select(worldY => WorldYToScreenY(editor, worldY, root.X))
                    .ToList();
                var objectScreenX = leftBounds
                    .Select(worldY =>
                    {
                        var anchor = contour
                            .Where(point => Math.Abs(point.Y - worldY) <= CadMath.MmPrecision)
                            .OrderBy(point => point.X)
                            .FirstOrDefault();
                        var worldX = Math.Abs(anchor.Y - worldY) <= CadMath.MmPrecision
                            ? anchor.X
                            : corners.TopLeft.X;
                        return WorldXToScreenX(editor, worldX, worldY);
                    })
                    .ToList();
                DrawLeftHeightDimensionChain(
                    context,
                    editor,
                    rect,
                    screenY,
                    leftBounds,
                    leftLengths,
                    totalHeightMm,
                    registerHits: true,
                    objectScreenX: objectScreenX);
            }
            else
            {
                DrawLeftHeightDimension(context, rect, totalHeightMm, editor, registerHits: true);
            }
        }
        else
        {
            DrawLeftHeightDimension(context, rect, totalHeightMm, editor, registerHits: true);
        }

        var rightBounds = new SortedSet<double>(CadMathComparer.Instance)
        {
            CadMath.RoundMm(minY),
            CadMath.RoundMm(corners.TopRight.Y),
            CadMath.RoundMm(maxY)
        }.ToList();
        var leftBaseHeight = CadMath.RoundMm(
            Math.Abs(corners.TopLeft.Y - corners.BottomLeft.Y));
        var rightHeight = Math.Abs(corners.TopRight.Y - corners.BottomRight.Y);
        var leftUpperDelta = CadMath.RoundMm(Math.Max(0, maxY - corners.TopLeft.Y));
        var rightUpperDelta = CadMath.RoundMm(Math.Max(0, maxY - corners.TopRight.Y));
        var isHeightSymmetric =
            Math.Abs(leftBaseHeight - rightHeight) <= CadMath.MmPrecision
            && Math.Abs(leftUpperDelta - rightUpperDelta) <= CadMath.MmPrecision;

        if (!isHeightSymmetric && rightBounds.Count > 2)
        {
            var rightLengths = rightBounds
                .Zip(rightBounds.Skip(1), (bottomY, topY) => CadMath.RoundMm(topY - bottomY))
                .ToList();
            var screenY = rightBounds
                .Select(worldY => WorldYToScreenY(editor, worldY, corners.TopRight.X))
                .ToList();
            var objectScreenX = rightBounds
                .Select(worldY =>
                {
                    var anchor = contour
                        .Where(point => Math.Abs(point.Y - worldY) <= CadMath.MmPrecision)
                        .OrderByDescending(point => point.X)
                        .FirstOrDefault();
                    var worldX = Math.Abs(anchor.Y - worldY) <= CadMath.MmPrecision
                        ? anchor.X
                        : corners.TopRight.X;
                    return WorldXToScreenX(editor, worldX, worldY);
                })
                .ToList();
            DrawRightHeightDimensionChain(
                context,
                editor,
                rect,
                screenY,
                objectScreenX,
                rightBounds,
                rightLengths,
                totalHeightMm,
                registerHits: true);
        }
        else if (!isHeightSymmetric)
        {
            DrawRightSideHeightDimension(
                context,
                rect,
                rightHeight,
                corners.BottomRight.Y,
                editor,
                registerHits: true);
        }

        // Верх: ортогональная проекция скошенного верхнего профиля.
        var topProjections = root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top);
        var topBounds = root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Top);
        // Если стыков нет — границы по X углов TL/TR.
        if (topBounds.Count < 2)
        {
            topBounds =
            [
                CadMath.RoundMm(corners.TopLeft.X),
                CadMath.RoundMm(corners.TopRight.X)
            ];
            topProjections =
            [
                CadMath.RoundMm(Math.Abs(corners.TopRight.X - corners.TopLeft.X))
            ];
        }

        if (topProjections.Count > 1 && topBounds.Count >= topProjections.Count + 1)
        {
            var topPoints = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
            var highestWorldY = topPoints.Max(point => point.Y);
            var dimensionAnchorScreenY = WorldYToScreenY(
                editor,
                highestWorldY,
                topPoints[0].X);
            var screenX = topPoints
                .Select(point => WorldXToScreenX(editor, point.X, point.Y))
                .ToList();
            var objectScreenY = topPoints
                .Select(point => WorldYToScreenY(editor, point.Y, point.X))
                .ToList();

            DrawTopWidthDimensionChain(
                context,
                editor,
                root,
                screenX,
                topProjections,
                dimensionAnchorScreenY,
                FrameProfileRail.Top,
                objectScreenY);
        }

        // Низ: проекция / стыки нижней рейки, иначе габарит.
        if (root.HasProfileRailJoints(FrameProfileRail.Bottom))
        {
            var bottomProjections = root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Bottom);
            var bottomBounds = root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Bottom);
            if (bottomProjections.Count > 1 && bottomBounds.Count >= 3)
            {
                var screenX = bottomBounds
                    .Select(worldX => WorldXToScreenX(editor, worldX, root.Y))
                    .ToList();
                DrawBottomWidthDimensionChain(
                    context,
                    editor,
                    screenX,
                    bottomBounds,
                    bottomProjections,
                    totalWidthMm,
                    ResolveBottomDimensionAnchorScreenY(editor, rect.Bottom),
                    registerHits: true);
            }
            else
            {
                DrawBottomOverallWidthDimension(context, editor, rect, totalWidthMm, registerHits: true);
            }
        }
        else
        {
            DrawBottomOverallWidthDimension(context, editor, rect, totalWidthMm, registerHits: true);
        }

        return true;
    }

    private sealed class CadMathComparer : IComparer<double>
    {
        public static readonly CadMathComparer Instance = new();

        public int Compare(double x, double y)
        {
            if (Math.Abs(x - y) <= CadMath.MmPrecision)
            {
                return 0;
            }

            return x.CompareTo(y);
        }
    }

    private static bool TryDrawProfileRailOuterDimensions(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode root,
        Rect rect,
        double totalWidthMm,
        double totalHeightMm)
    {
        var hasTop = root.HasProfileRailJoints(FrameProfileRail.Top);
        var hasBottom = root.HasProfileRailJoints(FrameProfileRail.Bottom);
        var hasLeft = root.HasProfileRailJoints(FrameProfileRail.Left);
        if (!hasTop && !hasBottom && !hasLeft && !root.HasProfileRailJoints(FrameProfileRail.Right))
        {
            return false;
        }

        // Высота слева: стыки левой рейки, иначе цепь по горизонтальным импостам, иначе габарит.
        if (hasLeft)
        {
            var lengths = root.GetOuterRailSegmentLengthsMm(FrameProfileRail.Left);
            var bounds = root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Left);
            if (lengths.Count > 1 && bounds.Count >= 3)
            {
                var screenY = bounds
                    .Select(worldY => WorldYToScreenY(editor, worldY, root.X))
                    .ToList();
                DrawLeftHeightDimensionChain(
                    context,
                    editor,
                    rect,
                    screenY,
                    bounds,
                    lengths,
                    totalHeightMm,
                    registerHits: true);
            }
            else
            {
                DrawLeftHeightDimension(context, rect, totalHeightMm, editor, registerHits: true);
            }
        }
        else if (TryCollectLeftHeightSegments(editor, root, out var segmentHeights, out var boundaryWorldY)
                 && segmentHeights.Count > 1
                 && boundaryWorldY.Count >= 3)
        {
            var screenBoundariesY = boundaryWorldY
                .Select(worldY => WorldYToScreenY(editor, worldY, root.X))
                .ToList();
            DrawLeftHeightDimensionChain(
                context,
                editor,
                rect,
                screenBoundariesY,
                boundaryWorldY,
                segmentHeights,
                totalHeightMm,
                registerHits: true);
        }
        else
        {
            DrawLeftHeightDimension(context, rect, totalHeightMm, editor, registerHits: true);
        }

        // Верх: только стыки верхней рейки (отдельная цепь, не заменяет низ).
        if (hasTop)
        {
            var topLengths = root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top);
            var topBounds = root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Top);
            if (topLengths.Count > 1 && topBounds.Count >= 3)
            {
                var topPoints = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
                var screenX = topBounds
                    .Select(worldX => WorldXToScreenX(editor, worldX, root.Y + root.Height))
                    .ToList();
                var objectScreenY = topPoints
                    .Select(point => WorldYToScreenY(editor, point.Y, point.X))
                    .ToList();
                DrawTopWidthDimensionChain(
                    context,
                    editor,
                    root,
                    screenX,
                    topLengths,
                    rect.Top,
                    FrameProfileRail.Top,
                    objectScreenY);
            }
        }

        // Низ: стыки нижней рейки, иначе цепь по вертикальным импостам, иначе габарит.
        if (hasBottom)
        {
            var bottomLengths = root.GetOuterRailSegmentLengthsMm(FrameProfileRail.Bottom);
            var bottomBounds = root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Bottom);
            if (bottomLengths.Count > 1 && bottomBounds.Count >= 3)
            {
                var screenX = bottomBounds
                    .Select(worldX => WorldXToScreenX(editor, worldX, root.Y))
                    .ToList();
                DrawBottomWidthDimensionChain(
                    context,
                    editor,
                    screenX,
                    bottomBounds,
                    bottomLengths,
                    totalWidthMm,
                    ResolveBottomDimensionAnchorScreenY(editor, rect.Bottom),
                    registerHits: true);
            }
            else
            {
                DrawBottomOverallWidthDimension(context, editor, rect, totalWidthMm, registerHits: true);
            }
        }
        else if (TryCollectBottomWidthSegments(editor, root, out var segmentWidths, out var boundaryWorldX)
                 && segmentWidths.Count > 1
                 && boundaryWorldX.Count >= 3)
        {
            var screenBoundariesX = boundaryWorldX
                .Select(worldX => WorldXToScreenX(editor, worldX, root.Y))
                .ToList();
            DrawBottomWidthDimensionChain(
                context,
                editor,
                screenBoundariesX,
                boundaryWorldX,
                segmentWidths,
                totalWidthMm,
                ResolveBottomDimensionAnchorScreenY(editor, rect.Bottom),
                registerHits: true);
        }
        else
        {
            DrawBottomOverallWidthDimension(context, editor, rect, totalWidthMm, registerHits: true);
        }

        return true;
    }

    private static void DrawTopWidthDimensionChain(
        DrawingContext context,
        CadEditorViewModel? editor,
        FrameNode root,
        IReadOnlyList<double> screenBoundaries,
        IReadOnlyList<double> segmentWidthsMm,
        double outerTopScreenY,
        FrameProfileRail rail,
        IReadOnlyList<double>? objectScreenY = null)
    {
        const double frameGap = DimensionFrameGap;
        const double segOffset = DimensionExtensionOffset;

        var yExtStart = outerTopScreenY - frameGap;
        var ySeg = outerTopScreenY - segOffset;

        for (var boundaryIndex = 0; boundaryIndex < screenBoundaries.Count; boundaryIndex++)
        {
            var startY = objectScreenY is { Count: > 0 } && boundaryIndex < objectScreenY.Count
                ? objectScreenY[boundaryIndex]
                : yExtStart;
            DrawVerticalExtensionToHorizontalDimension(
                context,
                editor,
                screenBoundaries[boundaryIndex],
                startY,
                ySeg);
        }

        for (var i = 0; i < segmentWidthsMm.Count; i++)
        {
            var x1 = screenBoundaries[i];
            var x2 = screenBoundaries[i + 1];
            DrawHorizontalDimensionSegment(context, editor, ySeg, x1, x2);

            var label = $"{segmentWidthsMm[i]:0}";
            var center = new Point((x1 + x2) / 2, ySeg + DimensionLabelClearancePx);
            if (!IsDimensionLabelHidden(editor, DimensionHitKind.ProfileRailSegment, i))
            {
                DrawCenteredText(context, label, center, DimensionTextBrush);
            }

            if (editor is not null)
            {
                var joints = root.GetProfileRailJoints(rail);
                if (joints.Count > 0)
                {
                    var moveRight = i < joints.Count;
                    var joint = moveRight ? joints[i] : joints[^1];
                    var (textBounds, hitRect) = MeasureHorizontalTextBounds(label, center);
                    editor.RegisterDimensionHit(new DimensionHitInfo
                    {
                        Kind = DimensionHitKind.ProfileRailSegment,
                        ValueMm = segmentWidthsMm[i],
                        TextBounds = textBounds,
                        ScreenRect = hitRect,
                        SegmentIndex = i,
                        ProfileRail = rail,
                        RailJointId = joint.Id,
                        MoveRightBoundary = moveRight
                    });
                }
            }
        }
    }

    private static bool FrameMatchesElement(FrameNode root, GeometryElement element)
    {
        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        const double tolerance = 1.0;
        return Math.Abs(root.X - originX) < tolerance &&
               Math.Abs(root.Y - originY) < tolerance &&
               Math.Abs(root.Width - width) < tolerance &&
               Math.Abs(root.Height - height) < tolerance;
    }

    private static bool TryCollectBottomWidthSegments(
        CadEditorViewModel editor,
        FrameNode root,
        out IReadOnlyList<double> segmentWidthsMm,
        out IReadOnlyList<double> boundaryWorldX) =>
        DimensionChainFromGridLines.TryCollectSegments(
            editor,
            root,
            GridLineOrientation.Vertical,
            out segmentWidthsMm,
            out boundaryWorldX);

    private static bool TryCollectLeftHeightSegments(
        CadEditorViewModel editor,
        FrameNode root,
        out IReadOnlyList<double> segmentHeightsMm,
        out IReadOnlyList<double> boundaryWorldY) =>
        DimensionChainFromGridLines.TryCollectSegments(
            editor,
            root,
            GridLineOrientation.Horizontal,
            out segmentHeightsMm,
            out boundaryWorldY);

    /// <summary>Публичный мост для <see cref="DimensionChainFromGridLines"/>.</summary>
    internal static bool TryBuildOpeningSegmentsForGrid(
        CadEditorViewModel editor,
        FrameNode root,
        List<double> rawBoundaries,
        bool alongX,
        out IReadOnlyList<double> segmentSizesMm,
        out IReadOnlyList<double> boundaryWorld) =>
        TryBuildOpeningSegmentsExcludingProfileBodies(
            editor,
            root,
            rawBoundaries,
            alongX,
            out segmentSizesMm,
            out boundaryWorld);

    /// <summary>
    /// Строит размерную цепочку Outer→Axis→Outer по абсолютным границам,
    /// исключая только тела переходных/вложенных профилей (не импостные «грани»).
    /// Длины сегментов — по осям/внешним краям, без вычета светового проёма.
    /// </summary>
    private static bool TryBuildOpeningSegmentsExcludingProfileBodies(
        CadEditorViewModel editor,
        FrameNode root,
        List<double> rawBoundaries,
        bool alongX,
        out IReadOnlyList<double> segmentSizesMm,
        out IReadOnlyList<double> boundaryWorld)
    {
        // Инвариант: boundaryWorld.Count == segmentSizesMm.Count + 1.
        // Границы добавляем только вместе с принятым сегментом — иначе после
        // исключения тела профиля и нулевого светового сегмента списки расходятся.
        var filteredBoundaries = new List<double>();
        var sizes = new List<double>();
        double? lastBoundary = null;

        for (var i = 0; i < rawBoundaries.Count - 1; i++)
        {
            var a = rawBoundaries[i];
            var b = rawBoundaries[i + 1];
            if (b - a <= CadMath.Epsilon)
            {
                continue;
            }

            if (IsStructuralProfileBodySpan(editor, root, a, b, alongX))
            {
                // Тело профиля не получает сегмент; следующий проём начнётся с новой опоры.
                continue;
            }

            var size = alongX
                ? ResolveBottomSegmentWidth(root, a, b)
                : ResolveLeftSegmentHeight(root, a, b);
            if (size <= CadMath.Epsilon)
            {
                continue;
            }

            if (lastBoundary is null)
            {
                filteredBoundaries.Add(a);
                lastBoundary = a;
            }
            else if (Math.Abs(lastBoundary.Value - a) > CadMath.MmPrecision)
            {
                // Разрыв после тела профиля: начинаем новую опору с `a`, не связывая
                // её с концом предыдущего проёма — иначе boundaries.Count ≠ sizes+1
                // и вся цепочка размеров отбрасывается.
                filteredBoundaries.Add(a);
                lastBoundary = a;
            }

            sizes.Add(size);
            filteredBoundaries.Add(b);
            lastBoundary = b;
        }

        // После разрывов списка boundaries может превысить sizes+1 за счёт стартов
        // новых проёмов. Пересобираем непрерывные цепочки: оставляем все сегменты,
        // сшивая границы так, что Count == sizes.Count + 1.
        if (sizes.Count == 0)
        {
            segmentSizesMm = Array.Empty<double>();
            boundaryWorld = Array.Empty<double>();
            return false;
        }

        if (filteredBoundaries.Count == sizes.Count + 1)
        {
            segmentSizesMm = sizes;
            boundaryWorld = filteredBoundaries;
            return true;
        }

        // Fallback при разрывах: границы [start0, end0, end1, ...] из пар сегментов,
        // каждый сегмент даёт свой label, геометрия якорей — фактические a/b.
        return TryRebuildSegmentChainAcrossGaps(
            rawBoundaries,
            editor,
            root,
            alongX,
            out segmentSizesMm,
            out boundaryWorld);
    }

    /// <summary>
    /// Собирает размерную цепочку при наличии «дыр» (тел профилей между проёмами).
    /// Сохраняет инвариант boundaries.Count == sizes.Count + 1, не вставляя
    /// лишние старты после пропущенного тела профиля.
    /// </summary>
    private static bool TryRebuildSegmentChainAcrossGaps(
        List<double> rawBoundaries,
        CadEditorViewModel editor,
        FrameNode root,
        bool alongX,
        out IReadOnlyList<double> segmentSizesMm,
        out IReadOnlyList<double> boundaryWorld)
    {
        var sizes = new List<double>();
        var boundaries = new List<double>();
        var started = false;

        for (var i = 0; i < rawBoundaries.Count - 1; i++)
        {
            var a = rawBoundaries[i];
            var b = rawBoundaries[i + 1];
            if (b - a <= CadMath.Epsilon || IsStructuralProfileBodySpan(editor, root, a, b, alongX))
            {
                continue;
            }

            var size = alongX
                ? ResolveBottomSegmentWidth(root, a, b)
                : ResolveLeftSegmentHeight(root, a, b);
            if (size <= CadMath.Epsilon)
            {
                continue;
            }

            if (!started)
            {
                boundaries.Add(a);
                started = true;
            }

            sizes.Add(size);
            boundaries.Add(b);
        }

        if (sizes.Count == 0 || boundaries.Count != sizes.Count + 1)
        {
            segmentSizesMm = Array.Empty<double>();
            boundaryWorld = Array.Empty<double>();
            return false;
        }

        segmentSizesMm = sizes;
        boundaryWorld = boundaries;
        return true;
    }

    /// <summary>
    /// Сегмент является толщиной конструктивного профиля (импост / переходной / стык ячеек),
    /// а не размером полезного проёма.
    /// </summary>
    private static bool IsStructuralProfileBodySpan(
        CadEditorViewModel editor,
        FrameNode root,
        double spanMin,
        double spanMax,
        bool alongX)
    {
        var span = spanMax - spanMin;
        if (span <= CadMath.Epsilon)
        {
            return true;
        }

        var mid = (spanMin + spanMax) / 2.0;

        if (IsImpostBodySpan(root, spanMin, spanMax, mid, alongX))
        {
            return true;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.TransitionFrame
                or GeometryElementKind.Frame
                or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            if (DummyFrame.IsDummyElement(element))
            {
                continue;
            }

            if (element.Kind == GeometryElementKind.TransitionFrame
                && IsTransitionProfileWallSpan(editor, element, spanMin, spanMax, mid, alongX))
            {
                return true;
            }

            if (element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                && IsNestedFrameWallSpan(editor, element, spanMin, spanMax, mid, alongX))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Толщина стенки переходного профиля (не весь AABB ячейки).
    /// </summary>
    private static bool IsTransitionProfileWallSpan(
        CadEditorViewModel editor,
        GeometryElement element,
        double spanMin,
        double spanMax,
        double mid,
        bool alongX)
    {
        if (!TransitionAdjacentFrameGeometry.TryResolveHostGeometry(
                editor,
                element,
                editor.FrameRoot,
                editor.FrameThickness,
                out var host))
        {
            return false;
        }

        var span = spanMax - spanMin;
        var tol = FrameNode.DefaultImpostProfileMm;

        if (alongX)
        {
            var probeY = (host.OuterBottom + host.OuterTop) / 2.0;
            var leftInner = host.InnerLeftFace(probeY);
            var rightInner = host.InnerRightFace(probeY);

            // Узкая вертикальная полоса (стойка на всю ячейку по толщине профиля).
            if (host.OuterWidth <= host.ProfileThickness + tol
                && mid >= host.OuterLeft - CadMath.MmPrecision
                && mid <= host.OuterRight + CadMath.MmPrecision
                && span <= host.OuterWidth + CadMath.MmPrecision
                && Math.Abs(spanMin - host.OuterLeft) <= tol
                && Math.Abs(spanMax - host.OuterRight) <= tol)
            {
                return true;
            }

            // Левая / правая стойка полого хоста.
            if (IsWallSpan(spanMin, spanMax, mid, host.OuterLeft, leftInner, tol)
                || IsWallSpan(spanMin, spanMax, mid, rightInner, host.OuterRight, tol))
            {
                return true;
            }
        }
        else
        {
            var probeX = (host.OuterLeft + host.OuterRight) / 2.0;
            var bottomInner = host.InnerBottomFace(probeX);
            var topInner = host.InnerTopFace(probeX);

            if (host.OuterHeight <= host.ProfileThickness + tol
                && mid >= host.OuterBottom - CadMath.MmPrecision
                && mid <= host.OuterTop + CadMath.MmPrecision
                && span <= host.OuterHeight + CadMath.MmPrecision
                && Math.Abs(spanMin - host.OuterBottom) <= tol
                && Math.Abs(spanMax - host.OuterTop) <= tol)
            {
                return true;
            }

            if (IsWallSpan(spanMin, spanMax, mid, host.OuterBottom, bottomInner, tol)
                || IsWallSpan(spanMin, spanMax, mid, topInner, host.OuterTop, tol))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Толщина стенки вложенной оконной коробки.
    /// </summary>
    private static bool IsNestedFrameWallSpan(
        CadEditorViewModel editor,
        GeometryElement element,
        double spanMin,
        double spanMax,
        double mid,
        bool alongX)
    {
        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return false;
        }

        var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
        if (inset <= CadMath.Epsilon)
        {
            return false;
        }

        var tol = FrameNode.DefaultImpostProfileMm;
        if (alongX)
        {
            return IsWallSpan(spanMin, spanMax, mid, originX, originX + inset, tol)
                   || IsWallSpan(spanMin, spanMax, mid, originX + width - inset, originX + width, tol);
        }

        return IsWallSpan(spanMin, spanMax, mid, originY, originY + inset, tol)
               || IsWallSpan(spanMin, spanMax, mid, originY + height - inset, originY + height, tol);
    }

    private static bool IsWallSpan(
        double spanMin,
        double spanMax,
        double mid,
        double wallMin,
        double wallMax,
        double tol)
    {
        if (wallMax - wallMin <= CadMath.Epsilon)
        {
            return false;
        }

        return mid >= wallMin - CadMath.MmPrecision
               && mid <= wallMax + CadMath.MmPrecision
               && Math.Abs(spanMin - wallMin) <= tol
               && Math.Abs(spanMax - wallMax) <= tol;
    }

    private static bool IsImpostBodySpan(
        FrameNode node,
        double spanMin,
        double spanMax,
        double mid,
        bool alongX)
    {
        var span = spanMax - spanMin;
        if (node.IsUniformSplit)
        {
            var cells = node.UniformCells;
            var thickness = node.GetResolvedImpostThicknessMm();
            if (thickness > CadMath.Epsilon
                && ((alongX && node.SplitOrientation == SplitOrientation.Vertical)
                    || (!alongX && node.SplitOrientation == SplitOrientation.Horizontal)))
            {
                for (var i = 0; i < cells.Count - 1; i++)
                {
                    double gapStart;
                    double gapEnd;
                    if (alongX)
                    {
                        gapStart = cells[i].X + cells[i].Width;
                        gapEnd = cells[i + 1].X;
                    }
                    else
                    {
                        gapStart = cells[i].Y + cells[i].Height;
                        gapEnd = cells[i + 1].Y;
                    }

                    if (mid >= gapStart - CadMath.MmPrecision
                        && mid <= gapEnd + CadMath.MmPrecision
                        && span <= Math.Max(thickness, gapEnd - gapStart) + CadMath.MmPrecision)
                    {
                        return true;
                    }
                }
            }

            foreach (var cell in cells)
            {
                if (IsImpostBodySpan(cell, spanMin, spanMax, mid, alongX))
                {
                    return true;
                }
            }

            return false;
        }

        if (!node.IsLeaf && node.ChildA is not null && node.ChildB is not null)
        {
            var thickness = node.GetResolvedImpostThicknessMm();
            if (thickness > CadMath.Epsilon)
            {
                if (alongX && node.SplitOrientation == SplitOrientation.Vertical)
                {
                    var gapStart = node.ChildA.X + node.ChildA.Width;
                    var gapEnd = node.ChildB.X;
                    if (mid >= gapStart - CadMath.MmPrecision
                        && mid <= gapEnd + CadMath.MmPrecision
                        && spanMax - spanMin <= Math.Max(thickness, gapEnd - gapStart) + CadMath.MmPrecision)
                    {
                        return true;
                    }
                }
                else if (!alongX && node.SplitOrientation == SplitOrientation.Horizontal)
                {
                    var gapStart = node.ChildA.Y + node.ChildA.Height;
                    var gapEnd = node.ChildB.Y;
                    if (mid >= gapStart - CadMath.MmPrecision
                        && mid <= gapEnd + CadMath.MmPrecision
                        && spanMax - spanMin <= Math.Max(thickness, gapEnd - gapStart) + CadMath.MmPrecision)
                    {
                        return true;
                    }
                }
            }

            if (IsImpostBodySpan(node.ChildA, spanMin, spanMax, mid, alongX)
                || IsImpostBodySpan(node.ChildB, spanMin, spanMax, mid, alongX))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryCollectSmartWidthSegmentsForNestedFrame(
        CadEditorViewModel editor,
        FrameNode root,
        GeometryElement element,
        out IReadOnlyList<double> segmentWidthsMm,
        out IReadOnlyList<double> boundaryWorldX,
        out IReadOnlyList<double> segmentHeightsMm,
        out IReadOnlyList<double> boundaryWorldY)
    {
        segmentWidthsMm = Array.Empty<double>();
        boundaryWorldX = Array.Empty<double>();
        segmentHeightsMm = Array.Empty<double>();
        boundaryWorldY = Array.Empty<double>();

        var hostLeaf = root.FindLeafAt((double)element.X, (double)element.Y);
        if (hostLeaf is null)
        {
            return false;
        }

        var dimensionNode = ResolveDimensionSplitNode(hostLeaf) ?? hostLeaf;
        var insets = hostLeaf.ResolveEdgeLightOpeningInsets();
        var innerLeft = hostLeaf.X + insets.Left;
        var innerRight = hostLeaf.X + hostLeaf.Width - insets.Right;

        var axes = new List<double>();
        if (root.IsFictitiousContainer)
        {
            CollectFictitiousGridVerticalDivisions(dimensionNode, dimensionNode, axes);
        }
        else
        {
            CollectVerticalImpostAxes(dimensionNode, axes);
        }

        DeduplicateSortedAxes(axes);

        if (axes.Count > 0)
        {
            var boundaries = new List<double> { innerLeft };
            boundaries.AddRange(axes);
            boundaries.Add(innerRight);

            if (TryBuildOpeningSegmentsExcludingProfileBodies(
                    editor,
                    root,
                    boundaries,
                    alongX: true,
                    out var widths,
                    out var filteredX))
            {
                segmentWidthsMm = widths;
                boundaryWorldX = filteredX;
            }
        }

        var innerBottom = hostLeaf.Y + insets.Bottom;
        var innerTop = hostLeaf.Y + hostLeaf.Height - insets.Top;
        var hAxes = new List<double>();
        if (root.IsFictitiousContainer)
        {
            CollectFictitiousGridHorizontalDivisions(dimensionNode, dimensionNode, hAxes);
        }
        else
        {
            CollectHorizontalImpostAxes(dimensionNode, hAxes);
        }

        DeduplicateSortedAxes(hAxes);

        if (hAxes.Count > 0)
        {
            var boundariesY = new List<double> { innerBottom };
            boundariesY.AddRange(hAxes);
            boundariesY.Add(innerTop);

            if (TryBuildOpeningSegmentsExcludingProfileBodies(
                    editor,
                    root,
                    boundariesY,
                    alongX: false,
                    out var heights,
                    out var filteredY))
            {
                segmentHeightsMm = heights;
                boundaryWorldY = filteredY;
            }
        }

        return segmentWidthsMm.Count > 0 || segmentHeightsMm.Count > 0;
    }

    private static FrameNode? ResolveDimensionSplitNode(FrameNode leaf)
    {
        for (var node = leaf; node.Parent is not null; node = node.Parent)
        {
            var parent = node.Parent;
            if (!parent.IsLeaf
                && (parent.IsUniformSplit || parent.SplitOrientation != SplitOrientation.None))
            {
                return parent;
            }
        }

        return null;
    }

    private static double ResolveBottomSegmentWidth(FrameNode root, double leftWorldX, double rightWorldX)
    {
        // Внешняя размерная цепь: Outer Edge → Impost Center Axis (без вычета фальца).
        _ = root;
        var width = rightWorldX - leftWorldX;
        return width <= CadMath.Epsilon ? 0 : CadMath.RoundMm(width);
    }

    private static double ResolveLeftSegmentHeight(FrameNode root, double bottomWorldY, double topWorldY)
    {
        // Размер ячейки считается между физическими гранями профилей.
        // Для опоры на ось горизонтального импоста вычитаем половину его
        // толщины с соответствующей стороны, чтобы тело профиля не попадало
        // ни в нижний, ни в верхний световой проём.
        var height = topWorldY - bottomWorldY;
        height -= ResolveHorizontalImpostHalfThicknessAt(root, bottomWorldY);
        height -= ResolveHorizontalImpostHalfThicknessAt(root, topWorldY);
        return height <= CadMath.Epsilon ? 0 : CadMath.RoundMm(height);
    }

    private static double ResolveHorizontalImpostHalfThicknessAt(FrameNode root, double axisWorldY)
    {
        foreach (var node in root.EnumerateInternalNodes())
        {
            if (node.IsUniformSplit && node.SplitOrientation == SplitOrientation.Horizontal)
            {
                for (var gap = 0; gap < node.UniformCells.Count - 1; gap++)
                {
                    if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gap, out var axis)
                        && CadMath.NearlyEqual(axis, axisWorldY))
                    {
                        return node.GetResolvedImpostThicknessMm() / 2.0;
                    }
                }

                continue;
            }

            if (node.SplitOrientation == SplitOrientation.Horizontal
                && node.TryGetHorizontalImpostAxisWorldY(out var binaryAxis)
                && CadMath.NearlyEqual(binaryAxis, axisWorldY))
            {
                return node.GetResolvedImpostThicknessMm() / 2.0;
            }
        }

        return 0;
    }

    private static void CollectHorizontalImpostAxes(FrameNode node, List<double> axes)
    {
        if (!node.IsLeaf)
        {
            if (node.IsUniformSplit && node.SplitOrientation == SplitOrientation.Horizontal)
            {
                for (var i = 0; i < node.UniformCells.Count - 1; i++)
                {
                    if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var axis))
                    {
                        axes.Add(axis);
                    }
                }
            }
            else if (node.SplitOrientation == SplitOrientation.Horizontal
                     && node.TryGetHorizontalImpostAxisWorldY(out var axis))
            {
                axes.Add(axis);
            }
        }

        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                CollectHorizontalImpostAxes(cell, axes);
            }

            return;
        }

        if (node.ChildA != null)
        {
            CollectHorizontalImpostAxes(node.ChildA, axes);
        }

        if (node.ChildB != null)
        {
            CollectHorizontalImpostAxes(node.ChildB, axes);
        }
    }

    private static void CollectVerticalImpostAxes(FrameNode node, List<double> axes)
    {
        if (!node.IsLeaf)
        {
            if (node.IsUniformSplit && node.SplitOrientation == SplitOrientation.Vertical)
            {
                for (var i = 0; i < node.UniformCells.Count - 1; i++)
                {
                    if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var axis))
                    {
                        axes.Add(axis);
                    }
                }
            }
            else if (node.SplitOrientation == SplitOrientation.Vertical
                     && node.TryGetVerticalImpostAxisWorldX(out var axis))
            {
                axes.Add(axis);
            }
        }

        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                CollectVerticalImpostAxes(cell, axes);
            }

            return;
        }

        if (node.ChildA != null)
        {
            CollectVerticalImpostAxes(node.ChildA, axes);
        }

        if (node.ChildB != null)
        {
            CollectVerticalImpostAxes(node.ChildB, axes);
        }
    }

    /// <summary>
    /// Точки деления фиктивной сетки по вертикали: внутренние X-границы ячеек
    /// (пунктирные импосты с нулевой толщиной совпадают с ними).
    /// </summary>
    private static void CollectFictitiousGridVerticalDivisions(
        FrameNode node,
        FrameNode root,
        List<double> axes)
    {
        if (node.IsTerminalLeaf)
        {
            AddInteriorDivisionAxis(axes, node.X, root.X, root.X + root.Width);
            AddInteriorDivisionAxis(axes, node.X + node.Width, root.X, root.X + root.Width);
            return;
        }

        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                CollectFictitiousGridVerticalDivisions(cell, root, axes);
            }

            return;
        }

        if (node.ChildA is not null)
        {
            CollectFictitiousGridVerticalDivisions(node.ChildA, root, axes);
        }

        if (node.ChildB is not null)
        {
            CollectFictitiousGridVerticalDivisions(node.ChildB, root, axes);
        }
    }

    /// <summary>
    /// Точки деления фиктивной сетки по горизонтали: внутренние Y-границы ячеек.
    /// </summary>
    private static void CollectFictitiousGridHorizontalDivisions(
        FrameNode node,
        FrameNode root,
        List<double> axes)
    {
        if (node.IsTerminalLeaf)
        {
            AddInteriorDivisionAxis(axes, node.Y, root.Y, root.Y + root.Height);
            AddInteriorDivisionAxis(axes, node.Y + node.Height, root.Y, root.Y + root.Height);
            return;
        }

        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                CollectFictitiousGridHorizontalDivisions(cell, root, axes);
            }

            return;
        }

        if (node.ChildA is not null)
        {
            CollectFictitiousGridHorizontalDivisions(node.ChildA, root, axes);
        }

        if (node.ChildB is not null)
        {
            CollectFictitiousGridHorizontalDivisions(node.ChildB, root, axes);
        }
    }

    private static void AddInteriorDivisionAxis(
        List<double> axes,
        double value,
        double outerMin,
        double outerMax)
    {
        var rounded = CadMath.RoundMm(value);
        if (rounded <= outerMin + CadMath.MmPrecision
            || rounded >= outerMax - CadMath.MmPrecision)
        {
            return;
        }

        axes.Add(rounded);
    }

    /// <summary>
    /// Опорные X-грани всех конструктивных профилей на канве (коробки, переходные,
    /// импосты-элементы), внутри габарита root. Агностично к типу профиля.
    /// </summary>
    private static void CollectStructuralProfileVerticalEdges(
        CadEditorViewModel editor,
        FrameNode root,
        List<double> axes)
    {
        var outerMin = root.X;
        var outerMax = root.X + root.Width;
        foreach (var element in editor.CanvasElements)
        {
            if (!IsStructuralDimensionProfileKind(element))
            {
                continue;
            }

            if (element.Kind == GeometryElementKind.TransitionFrame
                && TransitionAdjacentFrameGeometry.TryResolveHostGeometry(
                    editor,
                    element,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var host))
            {
                if (host.OuterTop <= root.Y + CadMath.MmPrecision
                    || host.OuterBottom >= root.Y + root.Height - CadMath.MmPrecision)
                {
                    continue;
                }

                var probeY = (host.OuterBottom + host.OuterTop) / 2.0;
                AddInteriorDivisionAxis(axes, host.OuterLeft, outerMin, outerMax);
                AddInteriorDivisionAxis(axes, host.InnerLeftFace(probeY), outerMin, outerMax);
                AddInteriorDivisionAxis(axes, host.InnerRightFace(probeY), outerMin, outerMax);
                AddInteriorDivisionAxis(axes, host.OuterRight, outerMin, outerMax);
                continue;
            }

            var (originX, originY, width, height) = GetWorldOriginBounds(element);
            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon
                || double.IsNaN(width) || double.IsNaN(height)
                || double.IsNaN(originX) || double.IsNaN(originY))
            {
                continue;
            }

            // Профиль должен пересекаться с root по Y, иначе чужие конструкции не учитываем.
            if (originY + height <= root.Y + CadMath.MmPrecision
                || originY >= root.Y + root.Height - CadMath.MmPrecision)
            {
                continue;
            }

            AddInteriorDivisionAxis(axes, originX, outerMin, outerMax);
            AddInteriorDivisionAxis(axes, originX + width, outerMin, outerMax);

            if (element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame)
            {
                var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
                if (inset > CadMath.Epsilon)
                {
                    AddInteriorDivisionAxis(axes, originX + inset, outerMin, outerMax);
                    AddInteriorDivisionAxis(axes, originX + width - inset, outerMin, outerMax);
                }
            }
        }
    }

    /// <summary>
    /// Опорные Y-грани всех конструктивных профилей на канве, внутри габарита root.
    /// </summary>
    private static void CollectStructuralProfileHorizontalEdges(
        CadEditorViewModel editor,
        FrameNode root,
        List<double> axes)
    {
        var outerMin = root.Y;
        var outerMax = root.Y + root.Height;
        foreach (var element in editor.CanvasElements)
        {
            if (!IsStructuralDimensionProfileKind(element))
            {
                continue;
            }

            if (element.Kind == GeometryElementKind.TransitionFrame
                && TransitionAdjacentFrameGeometry.TryResolveHostGeometry(
                    editor,
                    element,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var host))
            {
                if (host.OuterRight <= root.X + CadMath.MmPrecision
                    || host.OuterLeft >= root.X + root.Width - CadMath.MmPrecision)
                {
                    continue;
                }

                var probeX = (host.OuterLeft + host.OuterRight) / 2.0;
                AddInteriorDivisionAxis(axes, host.OuterBottom, outerMin, outerMax);
                AddInteriorDivisionAxis(axes, host.InnerBottomFace(probeX), outerMin, outerMax);
                AddInteriorDivisionAxis(axes, host.InnerTopFace(probeX), outerMin, outerMax);
                AddInteriorDivisionAxis(axes, host.OuterTop, outerMin, outerMax);
                continue;
            }

            var (originX, originY, width, height) = GetWorldOriginBounds(element);
            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon
                || double.IsNaN(width) || double.IsNaN(height)
                || double.IsNaN(originX) || double.IsNaN(originY))
            {
                continue;
            }

            if (originX + width <= root.X + CadMath.MmPrecision
                || originX >= root.X + root.Width - CadMath.MmPrecision)
            {
                continue;
            }

            AddInteriorDivisionAxis(axes, originY, outerMin, outerMax);
            AddInteriorDivisionAxis(axes, originY + height, outerMin, outerMax);

            if (element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame)
            {
                var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
                if (inset > CadMath.Epsilon)
                {
                    AddInteriorDivisionAxis(axes, originY + inset, outerMin, outerMax);
                    AddInteriorDivisionAxis(axes, originY + height - inset, outerMin, outerMax);
                }
            }
        }
    }

    private static bool IsStructuralDimensionProfileKind(GeometryElement element) =>
        element.Kind is GeometryElementKind.Frame
            or GeometryElementKind.ArchFrame
            or GeometryElementKind.TransitionFrame
            or GeometryElementKind.DummyFrame
            or GeometryElementKind.ArchDummyFrame
            or GeometryElementKind.TriangleDummyFrame
            or GeometryElementKind.RoundDummyFrame;

    private static void DeduplicateSortedAxes(List<double> axes)
    {
        if (axes.Count == 0)
        {
            return;
        }

        axes.Sort();
        for (var i = axes.Count - 1; i > 0; i--)
        {
            if (Math.Abs(axes[i] - axes[i - 1]) <= CadMath.MmPrecision)
            {
                axes.RemoveAt(i);
            }
        }
    }

    private static double WorldXToScreenX(CadEditorViewModel editor, double worldX, double worldBottomY)
    {
        return ToScreenPoint(editor, worldX, worldBottomY).X;
    }

    private static double WorldYToScreenY(CadEditorViewModel editor, double worldY, double worldLeftX)
    {
        return ToScreenPoint(editor, worldLeftX, worldY).Y;
    }

    private static double WorldMmToScreenPx(CadEditorViewModel? editor, double worldMm)
    {
        if (editor?.Viewport is null)
        {
            return 6;
        }

        var origin = editor.Viewport.WorldToScreen(new CadPoint(0, 0));
        var offset = editor.Viewport.WorldToScreen(new CadPoint(0, worldMm));
        return Math.Max(1, Math.Abs(offset.Y - origin.Y));
    }

    private static double ResolveDimensionOvershootPx(CadEditorViewModel? editor) =>
        Math.Clamp(WorldMmToScreenPx(editor, DimensionOvershootWorldMm), 5, 7);

    private static double ResolveDimensionTickHalfPx(CadEditorViewModel? editor) =>
        Math.Clamp(ResolveDimensionOvershootPx(editor) + 1, 5, 6);

    private static void DrawArchitecturalTick(DrawingContext context, CadEditorViewModel? editor, Point intersection)
    {
        var half = ResolveDimensionTickHalfPx(editor);
        context.DrawLine(
            DimensionTickPen,
            new Point(intersection.X - half, intersection.Y + half),
            new Point(intersection.X + half, intersection.Y - half));
    }

    private static void DrawVerticalExtensionToHorizontalDimension(
        DrawingContext context,
        CadEditorViewModel? editor,
        double boundaryScreenX,
        double objectScreenY,
        double dimensionScreenY)
    {
        var intersection = new Point(boundaryScreenX, dimensionScreenY);
        var direction = Math.Sign(dimensionScreenY - objectScreenY);
        if (direction == 0)
        {
            direction = 1;
        }
        context.DrawLine(
            DimensionPen,
            new Point(boundaryScreenX, objectScreenY),
            new Point(
                boundaryScreenX,
                dimensionScreenY + direction * DimensionJointOverlapPx));
        DrawArchitecturalTick(context, editor, intersection);
    }

    private static void DrawHorizontalExtensionToVerticalDimension(
        DrawingContext context,
        CadEditorViewModel? editor,
        double objectScreenX,
        double boundaryScreenY,
        double dimensionScreenX)
    {
        var intersection = new Point(dimensionScreenX, boundaryScreenY);
        var direction = Math.Sign(dimensionScreenX - objectScreenX);
        if (direction == 0)
        {
            direction = 1;
        }
        context.DrawLine(
            DimensionPen,
            new Point(objectScreenX, boundaryScreenY),
            new Point(
                dimensionScreenX + direction * DimensionJointOverlapPx,
                boundaryScreenY));
        DrawArchitecturalTick(context, editor, intersection);
    }

    private static void DrawHorizontalDimensionSegment(
        DrawingContext context,
        CadEditorViewModel? editor,
        double screenY,
        double screenX1,
        double screenX2)
    {
        var left = Math.Min(screenX1, screenX2);
        var right = Math.Max(screenX1, screenX2);
        context.DrawLine(
            DimensionPen,
            new Point(left, screenY),
            new Point(right, screenY));
    }

    private static void DrawVerticalDimensionSegment(
        DrawingContext context,
        CadEditorViewModel? editor,
        double screenX,
        double screenY1,
        double screenY2)
    {
        var top = Math.Min(screenY1, screenY2);
        var bottom = Math.Max(screenY1, screenY2);
        context.DrawLine(
            DimensionPen,
            new Point(screenX, top),
            new Point(screenX, bottom));
    }

    private static bool IsDimensionLabelHidden(CadEditorViewModel? editor, DimensionHitKind kind, int segmentIndex = 0) =>
        editor is { } vm && vm.IsDimensionLabelBeingEdited(kind, segmentIndex);

    public static (double Width, double Height) MeasureDimensionLabelText(string text)
    {
        var formatted = CreateDimensionFormattedText(text, vertical: false);
        return (formatted.Width, formatted.Height);
    }

    private static FormattedText CreateDimensionFormattedText(string text, bool vertical = false, IBrush? brush = null) =>
        new(
            text,
            vertical
                ? System.Globalization.CultureInfo.InvariantCulture
                : System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            DimensionLabelFontSize,
            brush ?? Brushes.Black);

    private static (Rect TextBounds, Rect HitRect) MeasureHorizontalTextBounds(string text, Point center)
    {
        var formatted = CreateDimensionFormattedText(text);
        var textBounds = new Rect(
            center.X - formatted.Width / 2,
            center.Y - formatted.Height / 2,
            formatted.Width,
            formatted.Height);
        var hitRect = new Rect(
            textBounds.X - DimensionHitPadding,
            textBounds.Y - DimensionHitPadding,
            textBounds.Width + DimensionHitPadding * 2,
            textBounds.Height + DimensionHitPadding * 2);
        return (textBounds, hitRect);
    }

    private static (Rect TextBounds, Rect HitRect) MeasureVerticalTextBounds(string text, Point centerOnDimLine)
    {
        var formatted = CreateDimensionFormattedText(text, vertical: true);
        var targetX = centerOnDimLine.X - DimensionLabelClearancePx;
        var targetY = centerOnDimLine.Y;
        var textBounds = new Rect(
            targetX - formatted.Height / 2,
            targetY - formatted.Width / 2,
            formatted.Height,
            formatted.Width);
        var hitRect = new Rect(
            textBounds.X - DimensionHitPadding,
            textBounds.Y - DimensionHitPadding,
            textBounds.Width + DimensionHitPadding * 2,
            textBounds.Height + DimensionHitPadding * 2);
        return (textBounds, hitRect);
    }

    private static void DrawVerticalTextAlongSide(DrawingContext context, string text, Point centerOnDimLine, IBrush brush)
    {
        var formatted = CreateDimensionFormattedText(text, vertical: true, brush);
        // Смещение влево от вертикальной размерной линии.
        var midPoint = new Point(centerOnDimLine.X - DimensionLabelClearancePx, centerOnDimLine.Y);

        using (context.PushTransform(Matrix.CreateTranslation(midPoint.X, midPoint.Y)))
        {
            using (context.PushTransform(Matrix.CreateRotation(-Math.PI / 2)))
            {
                DrawDimensionLabelBackdrop(context, formatted);
                context.DrawText(
                    formatted,
                    new Point(-formatted.Width / 2, -formatted.Height / 2));
            }
        }
    }

    private static void DrawTextAlongLine(
        DrawingContext context,
        string text,
        Point center,
        Point lineStart,
        Point lineEnd,
        IBrush brush,
        double perpendicularOffsetPx = 0)
    {
        var dx = lineEnd.X - lineStart.X;
        var dy = lineEnd.Y - lineStart.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length > 1e-6 && Math.Abs(perpendicularOffsetPx) > 1e-6)
        {
            var offsetX = dy / length * perpendicularOffsetPx;
            var offsetY = -dx / length * perpendicularOffsetPx;
            center = new Point(center.X + offsetX, center.Y + offsetY);
        }

        var angle = Math.Atan2(dy, dx);
        var formatted = CreateDimensionFormattedText(text, brush: brush);

        using (context.PushTransform(Matrix.CreateTranslation(center.X, center.Y)))
        {
            using (context.PushTransform(Matrix.CreateRotation(angle)))
            {
                DrawDimensionLabelBackdrop(context, formatted);
                context.DrawText(
                    formatted,
                    new Point(-formatted.Width / 2, -formatted.Height / 2));
            }
        }
    }

    private static void DrawCenteredText(DrawingContext context, string text, Point center, IBrush brush)
    {
        var formatted = CreateDimensionFormattedText(text, brush: brush);
        DrawDimensionLabelBackdrop(context, formatted, center);
        context.DrawText(
            formatted,
            new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }

    /// <summary>
    /// Белая подложка под цифрой — размерная линия визуально разрывается под текстом.
    /// </summary>
    private static void DrawDimensionLabelBackdrop(
        DrawingContext context,
        FormattedText formatted,
        Point? center = null)
    {
        Rect backdrop;
        if (center is { } c)
        {
            backdrop = new Rect(
                c.X - formatted.Width / 2 - DimensionLabelBackdropPadX,
                c.Y - formatted.Height / 2 - DimensionLabelBackdropPadY,
                formatted.Width + DimensionLabelBackdropPadX * 2,
                formatted.Height + DimensionLabelBackdropPadY * 2);
        }
        else
        {
            // В локальных координатах после Rotate/Translate (центр в 0,0).
            backdrop = new Rect(
                -formatted.Width / 2 - DimensionLabelBackdropPadX,
                -formatted.Height / 2 - DimensionLabelBackdropPadY,
                formatted.Width + DimensionLabelBackdropPadX * 2,
                formatted.Height + DimensionLabelBackdropPadY * 2);
        }

        context.DrawRectangle(DimensionLabelBackdropBrush, null, backdrop);
    }

    private static Rect MeasureCenteredTextRect(string text, Point center, double padding = 6)
    {
        var (textBounds, hitRect) = MeasureHorizontalTextBounds(text, center);
        if (Math.Abs(padding - DimensionHitPadding) < 0.01)
        {
            return hitRect;
        }

        return new Rect(
            textBounds.X - padding,
            textBounds.Y - padding,
            textBounds.Width + padding * 2,
            textBounds.Height + padding * 2);
    }

    private static Rect MeasureVerticalTextRect(string text, Point centerOnDimLine, double padding = 6)
    {
        var (textBounds, hitRect) = MeasureVerticalTextBounds(text, centerOnDimLine);
        if (Math.Abs(padding - DimensionHitPadding) < 0.01)
        {
            return hitRect;
        }

        return new Rect(
            textBounds.X - padding,
            textBounds.Y - padding,
            textBounds.Width + padding * 2,
            textBounds.Height + padding * 2);
    }

    private static bool TryParseDimensionValue(string label, out double valueMm)
    {
        var normalized = label.Trim().Replace(',', '.');
        return double.TryParse(
            normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out valueMm);
    }

    private static ImpostTarget? TryResolveVerticalImpostForSegment(
        FrameNode root,
        IReadOnlyList<double> boundaries,
        int segmentIndex)
    {
        var impostCount = boundaries.Count - 2;
        if (impostCount <= 0)
        {
            return null;
        }

        if (segmentIndex < impostCount)
        {
            return FindVerticalImpostAtAxis(root, boundaries[segmentIndex + 1]);
        }

        if (segmentIndex == impostCount)
        {
            return FindVerticalImpostAtAxis(root, boundaries[segmentIndex]);
        }

        return null;
    }

    private static ImpostTarget? FindVerticalImpostAtAxis(FrameNode root, double axisWorldX)
    {
        const double tolerance = 1.5;
        ImpostTarget? match = null;
        var bestDistance = double.MaxValue;

        foreach (var node in root.EnumerateInternalNodes())
        {
            if (node.SplitOrientation != SplitOrientation.Vertical)
            {
                continue;
            }

            if (node.IsUniformSplit)
            {
                for (var gapIndex = 0; gapIndex < node.UniformCells.Count - 1; gapIndex++)
                {
                    if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var axisX))
                    {
                        continue;
                    }

                    var distance = Math.Abs(axisX - axisWorldX);
                    if (distance > tolerance || distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    match = new ImpostTarget
                    {
                        SplitNode = node,
                        UniformGapIndex = gapIndex
                    };
                }

                continue;
            }

            if (!node.TryGetVerticalImpostAxisWorldX(out var binaryAxisX))
            {
                continue;
            }

            var binaryDistance = Math.Abs(binaryAxisX - axisWorldX);
            if (binaryDistance > tolerance || binaryDistance >= bestDistance)
            {
                continue;
            }

            bestDistance = binaryDistance;
            match = new ImpostTarget { SplitNode = node };
        }

        return match;
    }

    private static ImpostTarget? TryResolveHorizontalImpostForSegment(
        FrameNode root,
        IReadOnlyList<double> boundaries,
        int segmentIndex)
    {
        var impostCount = boundaries.Count - 2;
        if (impostCount <= 0)
        {
            return null;
        }

        if (segmentIndex < impostCount)
        {
            return FindHorizontalImpostAtAxis(root, boundaries[segmentIndex + 1]);
        }

        if (segmentIndex == impostCount)
        {
            return FindHorizontalImpostAtAxis(root, boundaries[segmentIndex]);
        }

        return null;
    }

    private static ImpostTarget? FindHorizontalImpostAtAxis(FrameNode root, double axisWorldY)
    {
        const double tolerance = 1.5;
        ImpostTarget? match = null;
        var bestDistance = double.MaxValue;
        var bestWidth = double.MaxValue;

        foreach (var node in root.EnumerateInternalNodes())
        {
            if (node.SplitOrientation != SplitOrientation.Horizontal)
            {
                continue;
            }

            if (node.IsUniformSplit)
            {
                for (var gapIndex = 0; gapIndex < node.UniformCells.Count - 1; gapIndex++)
                {
                    if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var axisY))
                    {
                        continue;
                    }

                    var distance = Math.Abs(axisY - axisWorldY);
                    if (distance > tolerance)
                    {
                        continue;
                    }

                    // При равной оси Y предпочитаем локальный (узкий) узел колонки.
                    if (distance > bestDistance + CadMath.MmPrecision
                        || (Math.Abs(distance - bestDistance) <= CadMath.MmPrecision
                            && node.Width >= bestWidth))
                    {
                        continue;
                    }

                    bestDistance = distance;
                    bestWidth = node.Width;
                    match = new ImpostTarget
                    {
                        SplitNode = node,
                        UniformGapIndex = gapIndex
                    };
                }

                continue;
            }

            if (!node.TryGetHorizontalImpostAxisWorldY(out var binaryAxisY))
            {
                continue;
            }

            var binaryDistance = Math.Abs(binaryAxisY - axisWorldY);
            if (binaryDistance > tolerance)
            {
                continue;
            }

            if (binaryDistance > bestDistance + CadMath.MmPrecision
                || (Math.Abs(binaryDistance - bestDistance) <= CadMath.MmPrecision
                    && node.Width >= bestWidth))
            {
                continue;
            }

            bestDistance = binaryDistance;
            bestWidth = node.Width;
            match = new ImpostTarget { SplitNode = node };
        }

        return match;
    }

    private static void DrawRoundDummyFrame(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (element.Shape == ConstructionShape.Circle)
        {
            ShapedFrameRenderer.DrawShapedFrame(
                context, editor, originX, originY, width, height, editor.FrameThickness,
                ConstructionShape.Circle, dashed: false);
            return;
        }

        var rect = ToScreenRect(editor, element);
        var pen = FrameVolumeRenderer.ResolveProfileOutlinePen(dashed: false);
        context.DrawEllipse(null, pen, rect.Center, rect.Width / 2, rect.Height / 2);
    }

    private static void DrawArchFrame(DrawingContext context, CadEditorViewModel editor, GeometryElement element, bool dashed = false)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        if (!ArchFrameDrawingHelper.TryResolveLayout(
                editor,
                element,
                out var originX,
                out var originY,
                out var width,
                out var boxHeight,
                out var topProfile))
        {
            return;
        }

        if (element.Kind == GeometryElementKind.ArchFrame)
        {
            FrameVolumeRenderer.Draw(
                context,
                editor,
                originX,
                originY,
                width,
                boxHeight,
                editor.FrameThickness,
                dashed: false,
                topProfile: topProfile);
            return;
        }

        FrameVolumeRenderer.DrawFictitiousArchOutline(
            context,
            editor,
            originX,
            originY,
            width,
            boxHeight,
            topProfile);
    }

    private static (double X, double Y, double W, double H) GetWorldOriginBounds(GeometryElement element)
    {
        var w = (double)element.WidthMm;
        var h = (double)element.HeightMm;
        var originX = (double)element.X - w / 2;
        var originY = (double)element.Y - h / 2;
        return (originX, originY, w, h);
    }

    private static Point ToScreenPoint(CadEditorViewModel editor, double worldX, double worldY)
    {
        var screen = editor.Viewport.WorldToScreen(new CadPoint(worldX, worldY));
        return new Point(screen.X, screen.Y);
    }

    private static void DrawTriangleFrame(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (element.Shape == ConstructionShape.Triangle)
        {
            ShapedFrameRenderer.DrawShapedFrame(
                context, editor, originX, originY, width, height, editor.FrameThickness,
                ConstructionShape.Triangle, dashed: false);
            return;
        }

        var rect = ToScreenRect(editor, element);
        var pen = FrameVolumeRenderer.ResolveProfileOutlinePen(dashed: false);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(rect.Left, rect.Bottom), isFilled: false);
            ctx.LineTo(new Point(rect.Right, rect.Bottom));
            ctx.LineTo(new Point(rect.Center.X, rect.Top));
            ctx.EndFigure(true);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private static void DrawSash(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(null, FramePen, rect);

        var inset = Math.Min(rect.Width, rect.Height) * 0.08;
        var inner = rect.Deflate(Math.Max(2, inset));
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(AccentFill, AccentPen, inner);

        var opening = element.OpeningType ?? SashOpeningType.Fixed;
        if (opening is SashOpeningType.Turn or SashOpeningType.LeftTurn or SashOpeningType.RightTurn
            or SashOpeningType.TiltTurn)
        {
            // Основание треугольника на стороне петель; вершина — на стороне открывания.
            var hingeLeft = opening is SashOpeningType.LeftTurn
                || (opening is not SashOpeningType.RightTurn
                    && element.HingeSide == SashHingeSide.Left);
            var apex = hingeLeft
                ? new Point(inner.Right, inner.Center.Y)
                : new Point(inner.Left, inner.Center.Y);
            var fromTop = hingeLeft ? inner.TopLeft : inner.TopRight;
            var fromBottom = hingeLeft ? inner.BottomLeft : inner.BottomRight;
            context.DrawLine(DashedAccentPen, fromTop, apex);
            context.DrawLine(DashedAccentPen, fromBottom, apex);
        }

        if (opening == SashOpeningType.Tilt || opening == SashOpeningType.TiltTurn)
        {
            // Фрамуга / откид: основание снизу, вершина сверху по центру.
            var midTop = new Point(inner.Center.X, inner.Top);
            context.DrawLine(DashedAccentPen, inner.BottomLeft, midTop);
            context.DrawLine(DashedAccentPen, inner.BottomRight, midTop);
        }
    }

    private static void DrawSlidingSash(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(AccentFill, FramePen, rect);

        var toLeft = element.OpeningType != SashOpeningType.SlideRight;
        var y = rect.Center.Y;
        var margin = Math.Min(rect.Width * 0.2, 20);
        var start = new Point(toLeft ? rect.Right - margin : rect.Left + margin, y);
        var end = new Point(toLeft ? rect.Left + margin : rect.Right - margin, y);
        context.DrawLine(AccentPen, start, end);
        DrawArrowHead(context, AccentPen, start, end);
    }

    private static void DrawGlazing(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement element,
        IReadOnlyList<GeometryElement> elements)
    {
        if (!SashGeometry.TryResolveGlassForGlazingElement(editor, element, elements, out var glass))
        {
            return;
        }

        if (glass.Width <= 2 || glass.Height <= 2)
        {
            return;
        }

        if (glass.IsArched)
        {
            DrawArchedGlazing(context, editor, glass, element, elements);
            return;
        }

        DrawPlanarGlazingWithBead(context, editor, glass, element, elements);
    }

    private static void DrawPlanarGlazingWithBead(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        GeometryElement? element = null,
        IReadOnlyList<GeometryElement>? allElements = null)
    {
        // Со штапиком (отдельный слой GlazingBeadRenderer) — заливка только внутри внутренних граней.
        if (GlazingBeadDrawRules.HasBead(element, allElements))
        {
            var insetMm = NurSapat.Cad.Core.GlazingBead.GlazingBeadSettings.DefaultBeadWidthMm
                + NurSapat.Cad.Core.GlazingBead.GlazingBeadSettings.TechnologyGapMm;
            var glassRect = ToScreenRectFromGlass(editor, glass, insetMm);
            if (glassRect.Width <= 1 || glassRect.Height <= 1)
            {
                return;
            }

            context.FillRectangle(GlazingFill, glassRect);
            GlassRenderer.DrawDiagonalReflections(context, glassRect);
            return;
        }

        var outerLoop = glass.BuildOuterContourLoop();
        FillGlazingContour(context, editor, outerLoop, GlazingFill, null);

        var beadInsetMm = SashGeometry.BeadProfileDrawThicknessMm;
        if (!glass.TryBuildInnerContourLoop(beadInsetMm, out var innerLoop))
        {
            StrokeGlazingContourLoop(context, editor, outerLoop, GlazingBeadPen);
            return;
        }

        StrokeGlazingContourLoop(context, editor, outerLoop, GlazingBeadPen);
        StrokeGlazingContourLoop(context, editor, innerLoop, GlazingBeadPen);

        var outerEdges = outerLoop.Edges;
        var innerEdges = innerLoop.Edges;
        for (var i = 0; i < outerEdges.Count && i < innerEdges.Count; i++)
        {
            context.DrawLine(
                GlazingBeadPen,
                WorldToScreen(editor, outerEdges[i].Start),
                WorldToScreen(editor, innerEdges[i].Start));
        }

        var innerRect = ToScreenRectFromGlass(editor, glass, beadInsetMm);
        if (innerRect.Width > 1 && innerRect.Height > 1)
        {
            GlassRenderer.DrawDiagonalReflections(context, innerRect);
        }
    }

    private static Rect ToScreenRectFromGlass(
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        double insetMm)
    {
        var left = glass.Left + insetMm;
        var bottom = glass.Bottom + insetMm;
        var right = glass.Right - insetMm;
        var top = glass.Top - insetMm;
        if (right <= left || top <= bottom)
        {
            return default;
        }

        var topLeft = editor.Viewport.WorldToScreen(new CadPoint(left, top));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(right, bottom));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }

    private static void DrawArchedGlazing(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        GeometryElement? element = null,
        IReadOnlyList<GeometryElement>? allElements = null)
    {
        if (GlazingBeadDrawRules.HasBead(element, allElements))
        {
            var insetMm = NurSapat.Cad.Core.GlazingBead.GlazingBeadSettings.DefaultBeadWidthMm
                + NurSapat.Cad.Core.GlazingBead.GlazingBeadSettings.TechnologyGapMm;
            if (glass.TryBuildInnerContourLoop(insetMm, out var beadInnerLoop))
            {
                FillGlazingContour(context, editor, beadInnerLoop, GlazingFill, null);
                DrawArchedGlazingReflections(context, editor, glass, insetMm);
                return;
            }
        }

        var outerLoop = glass.BuildOuterContourLoop();
        FillGlazingContour(context, editor, outerLoop, GlazingFill, null);

        var beadInsetMm = SashGeometry.BeadProfileDrawThicknessMm;
        if (!glass.TryBuildInnerContourLoop(beadInsetMm, out var innerLoop))
        {
            StrokeGlazingContourLoop(context, editor, outerLoop, GlazingBeadPen);
            return;
        }

        StrokeArchedGlazingBeadSegments(context, editor, glass, outerLoop, innerLoop);
        DrawArchedGlazingBeadMiters(context, editor, glass, beadInsetMm);
        DrawArchedGlazingReflections(context, editor, glass, beadInsetMm);
    }

    private static void StrokeArchedGlazingBeadSegments(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        ProfileContourLoop outerLoop,
        ProfileContourLoop innerLoop)
    {
        if (!glass.IsArched)
        {
            StrokeGlazingContourLoop(context, editor, outerLoop, GlazingBeadPen);
            StrokeGlazingContourLoop(context, editor, innerLoop, GlazingBeadPen);
            return;
        }

        var outerEdges = outerLoop.Edges;
        var innerEdges = innerLoop.Edges;
        for (var i = 0; i < outerEdges.Count && i < innerEdges.Count; i++)
        {
            var outerEdge = outerEdges[i];
            var innerEdge = innerEdges[i];
            if (outerEdge.Kind == ProfileEdgeKind.Line)
            {
                context.DrawLine(
                    GlazingBeadPen,
                    WorldToScreen(editor, outerEdge.Start),
                    WorldToScreen(editor, outerEdge.End));
            }
            else
            {
                var previous = WorldToScreen(editor, outerEdge.Start);
                foreach (var worldPoint in CircularArc.TessellateSpringingArc(
                             outerEdge.ArcCenter,
                             outerEdge.ArcRadius,
                             outerEdge.ArcStartAngleRad,
                             outerEdge.ArcEndAngleRad))
                {
                    var current = WorldToScreen(editor, worldPoint);
                    context.DrawLine(GlazingBeadPen, previous, current);
                    previous = current;
                }
            }

            if (innerEdge.Kind == ProfileEdgeKind.Line)
            {
                context.DrawLine(
                    GlazingBeadPen,
                    WorldToScreen(editor, innerEdge.Start),
                    WorldToScreen(editor, innerEdge.End));
            }
            else
            {
                var previous = WorldToScreen(editor, innerEdge.Start);
                foreach (var worldPoint in CircularArc.TessellateSpringingArc(
                             innerEdge.ArcCenter,
                             innerEdge.ArcRadius,
                             innerEdge.ArcStartAngleRad,
                             innerEdge.ArcEndAngleRad))
                {
                    var current = WorldToScreen(editor, worldPoint);
                    context.DrawLine(GlazingBeadPen, previous, current);
                    previous = current;
                }
            }
        }
    }

    private static void FillGlazingContour(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IBrush fill,
        IPen? pen)
    {
        var points = loop.Tessellate();
        if (points.Count < 3)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(WorldToScreen(editor, points[0]), isFilled: true);
            for (var i = 1; i < points.Count; i++)
            {
                ctx.LineTo(WorldToScreen(editor, points[i]));
            }

            ctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static void StrokeGlazingContourLoop(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IPen pen)
    {
        foreach (var edge in loop.Edges)
        {
            if (edge.Kind == ProfileEdgeKind.Line)
            {
                context.DrawLine(pen, WorldToScreen(editor, edge.Start), WorldToScreen(editor, edge.End));
                continue;
            }

            var previous = WorldToScreen(editor, edge.Start);
            foreach (var worldPoint in CircularArc.TessellateSpringingArc(
                         edge.ArcCenter,
                         edge.ArcRadius,
                         edge.ArcStartAngleRad,
                         edge.ArcEndAngleRad))
            {
                var current = WorldToScreen(editor, worldPoint);
                context.DrawLine(pen, previous, current);
                previous = current;
            }
        }
    }

    private static void DrawArchedGlazingBeadMiters(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        double beadInsetMm)
    {
        if (!glass.TryBuildInnerContourLoop(beadInsetMm, out var innerLoop))
        {
            return;
        }

        var outerLoop = glass.BuildOuterContourLoop();
        var outerEdges = outerLoop.Edges;
        var innerEdges = innerLoop.Edges;
        for (var i = 0; i < outerEdges.Count && i < innerEdges.Count; i++)
        {
            context.DrawLine(
                GlazingBeadPen,
                WorldToScreen(editor, outerEdges[i].Start),
                WorldToScreen(editor, innerEdges[i].Start));
        }
    }

    private static void DrawArchedGlazingReflections(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        double beadInsetMm)
    {
        if (!glass.TryBuildInnerContourLoop(beadInsetMm, out _))
        {
            return;
        }

        var topLeft = WorldToScreen(editor, new CadPoint(glass.Left, glass.ResolveTopAtX(glass.Left)));
        var topRight = WorldToScreen(editor, new CadPoint(glass.Right, glass.ResolveTopAtX(glass.Right)));
        var bottomLeft = WorldToScreen(editor, glass.BottomLeft);
        var bottomRight = WorldToScreen(editor, glass.BottomRight);
        var minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        var maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        var maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
        GlassRenderer.DrawDiagonalReflections(context, new Rect(minX, minY, maxX - minX, maxY - minY));
    }

    /// <summary>Диагональные стыки штапика 45° от угла светового проёма к углу внутреннего контура.</summary>
    private static void DrawGlazingBeadCornerMiters(DrawingContext context, Rect outer, Rect inner)
    {
        context.DrawLine(GlazingBeadPen, new Point(outer.Left, outer.Top), new Point(inner.Left, inner.Top));
        context.DrawLine(GlazingBeadPen, new Point(outer.Right, outer.Top), new Point(inner.Right, inner.Top));
        context.DrawLine(GlazingBeadPen, new Point(outer.Left, outer.Bottom), new Point(inner.Left, inner.Bottom));
        context.DrawLine(GlazingBeadPen, new Point(outer.Right, outer.Bottom), new Point(inner.Right, inner.Bottom));
    }

    private static void DrawCorrugation(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(null, ThinPen, rect);
        DrawHatch(context, rect, spacing: 6, diagonal: false);
    }

    private static void DrawMosquitoNet(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(null, ThinPen, rect);
        DrawHatch(context, rect, spacing: 8, diagonal: true);
    }

    private static void DrawPointMarker(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var fill = element.Kind == GeometryElementKind.Handle ? AccentBrush : AccentFill;
        context.DrawEllipse(fill, AccentPen, center, MarkerRadiusPx, MarkerRadiusPx);

        if (element.Kind == GeometryElementKind.Handle)
        {
            context.DrawLine(FramePen, center, new Point(center.X, center.Y + MarkerRadiusPx * 1.5));
        }
    }

    private static void DrawBlindOverlay(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(48, 120, 120, 120)), ThinPen, rect);
        var vertical = element.SourceToolId == CadCreationToolCatalog.BlindVertical;
        DrawHatch(context, rect, spacing: vertical ? 10 : 8, diagonal: !vertical);
    }

    private static void DrawAccessory(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (element.SourceToolId is CadCreationToolCatalog.BlindVertical or CadCreationToolCatalog.BlindHorizontal)
        {
            DrawBlindOverlay(context, editor, element);
            return;
        }

        if (element.SourceToolId == CadCreationToolCatalog.Cornice)
        {
            var rect = ToScreenRect(editor, element);
            context.DrawRectangle(AccentFill, FramePen, rect);
            return;
        }

        var center = ToScreenPoint(editor, element);
        var r = MarkerRadiusPx;
        var diamond = new[]
        {
            new Point(center.X, center.Y - r),
            new Point(center.X + r, center.Y),
            new Point(center.X, center.Y + r),
            new Point(center.X - r, center.Y)
        };
        for (var i = 0; i < diamond.Length; i++)
        {
            context.DrawLine(AccentPen, diamond[i], diamond[(i + 1) % diamond.Length]);
        }
    }

    private static void DrawInstallation(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var r = MarkerRadiusPx;
        var top = new Point(center.X, center.Y - r);
        var left = new Point(center.X - r, center.Y + r);
        var right = new Point(center.X + r, center.Y + r);
        context.DrawLine(FramePen, top, left);
        context.DrawLine(FramePen, top, right);
        context.DrawLine(FramePen, left, right);
        context.DrawLine(ThinPen, new Point(left.X, left.Y + 3), new Point(right.X, right.Y + 3));
    }

    private static (double MinX, double MaxX, double MinY, double MaxY) ResolveFrameBounds(CadEditorViewModel editor, CadPoint point)
    {
        var rects = editor.Document.Rectangles;
        var containing = rects.Where(r => r.Contains(point)).ToList();
        var source = containing.Count > 0 ? containing : rects.ToList();

        if (source.Count == 0)
        {
            var half = FallbackSpanMm / 2;
            return (point.X - half, point.X + half, point.Y - half, point.Y + half);
        }

        var minX = source.Min(r => Math.Min(r.X, r.X + r.Width));
        var maxX = source.Max(r => Math.Max(r.X, r.X + r.Width));
        var minY = source.Min(r => Math.Min(r.Y, r.Y - r.Height));
        var maxY = source.Max(r => Math.Max(r.Y, r.Y - r.Height));
        return (minX, maxX, minY, maxY);
    }

    private static Rect ToScreenRect(CadEditorViewModel editor, GeometryElement element)
    {
        if (editor?.Viewport == null)
        {
            return default;
        }

        var cx = (double)element.X;
        var cy = (double)element.Y;
        var halfW = (double)element.WidthMm / 2;
        var halfH = (double)element.HeightMm / 2;
        if (halfW <= 0 || halfH <= 0)
        {
            return default;
        }

        var topLeft = editor.Viewport.WorldToScreen(new CadPoint(cx - halfW, cy + halfH));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(cx + halfW, cy - halfH));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint worldPoint)
    {
        var screen = editor.Viewport!.WorldToScreen(worldPoint);
        return new Point(screen.X, screen.Y);
    }

    private static Point ToScreenPoint(CadEditorViewModel editor, GeometryElement element)
    {
        var screen = editor.Viewport.WorldToScreen(new CadPoint((double)element.X, (double)element.Y));
        return new Point(screen.X, screen.Y);
    }

    private static void DrawWorldLine(DrawingContext context, CadEditorViewModel editor, IPen pen, CadPoint start, CadPoint end)
    {
        var p1 = editor.Viewport.WorldToScreen(start);
        var p2 = editor.Viewport.WorldToScreen(end);
        context.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
    }

    private static void DrawHatch(DrawingContext context, Rect rect, double spacing, bool diagonal)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = context.PushClip(rect);

        if (diagonal)
        {
            var span = rect.Width + rect.Height;
            for (double offset = -rect.Height; offset < span; offset += spacing)
            {
                context.DrawLine(HatchPen,
                    new Point(rect.Left + offset, rect.Top),
                    new Point(rect.Left + offset - rect.Height, rect.Bottom));
                context.DrawLine(HatchPen,
                    new Point(rect.Left + offset, rect.Top),
                    new Point(rect.Left + offset + rect.Height, rect.Bottom));
            }
        }
        else
        {
            for (var y = rect.Top + spacing; y < rect.Bottom; y += spacing)
            {
                context.DrawLine(HatchPen, new Point(rect.Left, y), new Point(rect.Right, y));
            }
        }
    }

    private static void DrawArrowHead(DrawingContext context, IPen pen, Point from, Point to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        const double size = 7;
        var left = new Point(to.X - ux * size - uy * size * 0.5, to.Y - uy * size + ux * size * 0.5);
        var right = new Point(to.X - ux * size + uy * size * 0.5, to.Y - uy * size - ux * size * 0.5);
        context.DrawLine(pen, to, left);
        context.DrawLine(pen, to, right);
    }
}
