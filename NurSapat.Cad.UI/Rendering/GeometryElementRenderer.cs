using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class GeometryElementRenderer
{
    private const double FallbackSpanMm = 2000;
    private const double MarkerRadiusPx = 8;

    private static readonly IBrush CadStrokeBrush = new SolidColorBrush(Color.Parse("#AEAEAE"));

    private static readonly IBrush DimensionColorBrush = new SolidColorBrush(Color.Parse("#C4C4C4"));
    private static readonly IBrush DimensionTextBrush = new SolidColorBrush(Color.Parse("#696969"));

    private static readonly IBrush FrameBrush = CadStrokeBrush;
    private static readonly IBrush GlazingFill = new SolidColorBrush(Color.FromArgb(24, 0xBF, 0xDB, 0xFE));
    private static readonly IPen GlazingBeadPen = new Pen(new SolidColorBrush(Color.Parse("#9CA3AF")), 1);
    private static readonly IPen GlazingReflectionPen = new Pen(
        new SolidColorBrush(Color.Parse("#4169E1")) { Opacity = 0.9 },
        1);
    private const double GlazingInnerInsetPx = 6;
    private static readonly IBrush AccentFill = new SolidColorBrush(Color.FromArgb(30, 0x6B, 0x72, 0x80));
    private static readonly IBrush AccentBrush = CadStrokeBrush;
    private static readonly IPen FramePen = new Pen(FrameBrush, 1.5);
    private static readonly IPen ThinPen = new Pen(FrameBrush, 1);
    private static readonly IPen AccentPen = ThinPen;

    private static readonly IPen DimensionPen = new Pen(DimensionColorBrush, 1.2);

    private static readonly IPen DashedPen = new Pen(FrameBrush, 1.5, new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen DashedAccentPen = DashedPen;
    private static readonly IPen HatchPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 0x1F, 0x29, 0x37)), 0.8);

    private const double DimensionFrameGap = 2;
    private const double DimensionExtensionOffset = 21;
    private const double DimensionOverallOffset = 42;
    private const double DimensionTickSize = 1.5;
    public const double DimensionLabelFontSize = 11;
    private const double DimensionHitPadding = 6;

    public static void DrawAll(DrawingContext context, CadEditorViewModel editor, IReadOnlyList<GeometryElement> elements)
    {
        if (editor == null)
        {
            return;
        }

        foreach (var element in elements)
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

        foreach (var element in elements)
        {
            if (element.Kind != GeometryElementKind.Glazing)
            {
                continue;
            }

            try
            {
                DrawGlazing(context, editor, element);
            }
            catch (Exception ex)
            {
                CadDiagnostics.LogWarning($"DrawGlazingLayer failed for {element.Id}", ex);
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
                context.DrawRectangle(null, DashedPen, ToScreenRect(editor, element));
                break;
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameTriangle:
                DrawTriangleFrame(context, editor, element);
                break;
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameArch:
                DrawArchFrame(context, editor, element, dashed: true);
                break;
            case NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameCircle:
                DrawRoundDummyFrame(context, editor, element);
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
            HeightMm = (decimal)height
        };
    }

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
            case GeometryElementKind.TransitionFrame:
                {
                    var (originX, originY, width, height) = GetWorldOriginBounds(element);
                    FrameVolumeRenderer.Draw(
                        context,
                        editor,
                        originX,
                        originY,
                        width,
                        height,
                        editor.FrameThickness,
                        dashed: false);
                    if (editor.ShowDimensions)
                    {
                        DrawFrameDimensions(context, editor, element);
                    }
                    break;
                }
            case GeometryElementKind.DummyFrame:
                {
                    var (originX, originY, width, height) = GetWorldOriginBounds(element);
                    FrameVolumeRenderer.Draw(
                        context,
                        editor,
                        originX,
                        originY,
                        width,
                        height,
                        editor.FrameThickness,
                        dashed: true);
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
            case GeometryElementKind.Corrugation:
                DrawCorrugation(context, editor, element);
                break;
            case GeometryElementKind.MosquitoNet:
                DrawMosquitoNet(context, editor, element);
                break;
            case GeometryElementKind.Handle:
            case GeometryElementKind.Hardware:
                DrawPointMarker(context, editor, element);
                break;
            case GeometryElementKind.Bead:
                DrawBead(context, editor, element);
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
        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;

        if (editor.FrameRoot != null &&
            FrameMatchesElement(editor.FrameRoot, element) &&
            TryCollectBottomWidthSegments(
                editor.FrameRoot,
                out var segmentWidths,
                out var boundaryWorldX))
        {
            DrawScreenDimensionsWithSegmentedBottom(
                context,
                editor,
                rect,
                element,
                segmentWidths,
                boundaryWorldX,
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
        var radius = width / 2;
        DrawScreenDimensions(context, rect, $"{width:0}", $"{height:0}", editor, registerHits: true);

        var (originX, originY, _, _) = GetWorldOriginBounds(element);
        var baseCenter = ToScreenPoint(editor, originX + width / 2, originY);
        var radialEnd = ToScreenPoint(editor, originX + width / 2 + radius * Math.Cos(Math.PI / 3), originY + radius * Math.Sin(Math.PI / 3));
        context.DrawLine(DimensionPen, baseCenter, radialEnd);
        var labelPoint = new Point((baseCenter.X + radialEnd.X) / 2, (baseCenter.Y + radialEnd.Y) / 2);
        DrawCenteredText(context, $"R={radius:0}", labelPoint, DimensionTextBrush);
    }

    private static void DrawTriangleDimensions(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var half = width / 2;

        const double frameGap = DimensionFrameGap;
        const double segOffset = DimensionExtensionOffset;
        const double overallOffset = DimensionOverallOffset;
        const double tick = DimensionTickSize;

        var xDim = rect.Left - segOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Top), new Point(xDim, rect.Top));
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Bottom), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim, rect.Top), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Top), new Point(xDim + tick, rect.Top));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Bottom), new Point(xDim + tick, rect.Bottom));
        DrawVerticalTextAlongSide(context, $"{height:0}", new Point(xDim - 3, rect.Center.Y), DimensionTextBrush);

        var midX = rect.Center.X;
        var yExtStart = rect.Bottom + frameGap;
        var ySeg = rect.Bottom + segOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, yExtStart), new Point(rect.Left, ySeg));
        context.DrawLine(DimensionPen, new Point(midX, yExtStart), new Point(midX, ySeg));
        context.DrawLine(DimensionPen, new Point(rect.Right, yExtStart), new Point(rect.Right, ySeg));
        context.DrawLine(DimensionPen, new Point(rect.Left, ySeg), new Point(midX, ySeg));
        context.DrawLine(DimensionPen, new Point(midX, ySeg), new Point(rect.Right, ySeg));
        context.DrawLine(DimensionPen, new Point(rect.Left, ySeg - tick), new Point(rect.Left, ySeg + tick));
        context.DrawLine(DimensionPen, new Point(midX, ySeg - tick), new Point(midX, ySeg + tick));
        context.DrawLine(DimensionPen, new Point(rect.Right, ySeg - tick), new Point(rect.Right, ySeg + tick));

        DrawCenteredText(context, $"{half:0}", new Point((rect.Left + midX) / 2, ySeg - 4), DimensionTextBrush);
        DrawCenteredText(context, $"{half:0}", new Point((midX + rect.Right) / 2, ySeg - 4), DimensionTextBrush);

        var yOverall = rect.Bottom + overallOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, ySeg + tick), new Point(rect.Left, yOverall));
        context.DrawLine(DimensionPen, new Point(rect.Right, ySeg + tick), new Point(rect.Right, yOverall));
        context.DrawLine(DimensionPen, new Point(rect.Left, yOverall), new Point(rect.Right, yOverall));
        context.DrawLine(DimensionPen, new Point(rect.Left, yOverall - tick), new Point(rect.Left, yOverall + tick));
        context.DrawLine(DimensionPen, new Point(rect.Right, yOverall - tick), new Point(rect.Right, yOverall + tick));

        DrawCenteredText(context, $"{width:0}", new Point(rect.Center.X, yOverall + 4), DimensionTextBrush);
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
        const double frameGap = DimensionFrameGap;
        const double dimOffset = DimensionExtensionOffset;
        const double tick = DimensionTickSize;

        var yExtStart = rect.Bottom + frameGap;
        var yDim = rect.Bottom + dimOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, yExtStart), new Point(rect.Left, yDim));
        context.DrawLine(DimensionPen, new Point(rect.Right, yExtStart), new Point(rect.Right, yDim));
        context.DrawLine(DimensionPen, new Point(rect.Left, yDim), new Point(rect.Right, yDim));
        context.DrawLine(DimensionPen, new Point(rect.Left, yDim - tick), new Point(rect.Left, yDim + tick));
        context.DrawLine(DimensionPen, new Point(rect.Right, yDim - tick), new Point(rect.Right, yDim + tick));

        var widthCenter = new Point(rect.Center.X, yDim + 4);
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
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Top), new Point(xDim, rect.Top));
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Bottom), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim, rect.Top), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Top), new Point(xDim + tick, rect.Top));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Bottom), new Point(xDim + tick, rect.Bottom));

        var heightCenter = new Point(xDim - 3, rect.Center.Y);
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

    private static void DrawScreenDimensionsWithSegmentedBottom(
        DrawingContext context,
        CadEditorViewModel editor,
        Rect rect,
        GeometryElement element,
        IReadOnlyList<double> segmentWidthsMm,
        IReadOnlyList<double> boundaryWorldX,
        double totalWidthMm,
        double heightMm,
        bool registerHits = false)
    {
        DrawLeftHeightDimension(context, rect, heightMm, editor, registerHits);

        var (_, originY, _, _) = GetWorldOriginBounds(element);
        var screenBoundaries = boundaryWorldX
            .Select(worldX => WorldXToScreenX(editor, worldX, originY))
            .ToList();

        DrawBottomWidthDimensionChain(
            context,
            editor,
            screenBoundaries,
            boundaryWorldX,
            segmentWidthsMm,
            totalWidthMm,
            rect.Bottom,
            registerHits);
    }

    private static void DrawLeftHeightDimension(
        DrawingContext context,
        Rect rect,
        double heightMm,
        CadEditorViewModel? editor = null,
        bool registerHits = false)
    {
        const double dimOffset = DimensionExtensionOffset;
        const double tick = DimensionTickSize;

        var xDim = rect.Left - dimOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Top), new Point(xDim, rect.Top));
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Bottom), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim, rect.Top), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Top), new Point(xDim + tick, rect.Top));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Bottom), new Point(xDim + tick, rect.Bottom));

        var label = $"{heightMm:0}";
        var heightCenter = new Point(xDim - 3, rect.Center.Y);
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

    private static void DrawBottomWidthDimensionChain(
        DrawingContext context,
        CadEditorViewModel? editor,
        IReadOnlyList<double> screenBoundaries,
        IReadOnlyList<double> boundaryWorldX,
        IReadOnlyList<double> segmentWidthsMm,
        double totalWidthMm,
        double outerBottomScreenY,
        bool registerHits = false)
    {
        const double frameGap = DimensionFrameGap;
        const double segOffset = DimensionExtensionOffset;
        const double overallOffset = DimensionOverallOffset;
        const double tick = DimensionTickSize;

        var yExtStart = outerBottomScreenY + frameGap;
        var ySeg = outerBottomScreenY + segOffset;

        foreach (var boundaryX in screenBoundaries)
        {
            context.DrawLine(DimensionPen, new Point(boundaryX, yExtStart), new Point(boundaryX, ySeg));
            context.DrawLine(DimensionPen, new Point(boundaryX, ySeg - tick), new Point(boundaryX, ySeg + tick));
        }

        var impostCount = boundaryWorldX.Count - 2;
        for (var i = 0; i < segmentWidthsMm.Count; i++)
        {
            var x1 = screenBoundaries[i];
            var x2 = screenBoundaries[i + 1];
            context.DrawLine(DimensionPen, new Point(x1, ySeg), new Point(x2, ySeg));

            var label = $"{segmentWidthsMm[i]:0}";
            var center = new Point((x1 + x2) / 2, ySeg - 4);
            if (!IsDimensionLabelHidden(editor, DimensionHitKind.SegmentWidth, i))
            {
                DrawCenteredText(context, label, center, DimensionTextBrush);
            }

            if (registerHits && editor?.FrameRoot != null)
            {
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
        context.DrawLine(DimensionPen, new Point(left, ySeg + tick), new Point(left, yOverall));
        context.DrawLine(DimensionPen, new Point(right, ySeg + tick), new Point(right, yOverall));
        context.DrawLine(DimensionPen, new Point(left, yOverall), new Point(right, yOverall));
        context.DrawLine(DimensionPen, new Point(left, yOverall - tick), new Point(left, yOverall + tick));
        context.DrawLine(DimensionPen, new Point(right, yOverall - tick), new Point(right, yOverall + tick));

        var overallLabel = $"{totalWidthMm:0}";
        var overallCenter = new Point((left + right) / 2, yOverall + 4);
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
        FrameNode root,
        out IReadOnlyList<double> segmentWidthsMm,
        out IReadOnlyList<double> boundaryWorldX)
    {
        var axes = new List<double>();
        CollectVerticalImpostAxes(root, axes);
        axes.Sort();

        if (axes.Count == 0)
        {
            segmentWidthsMm = Array.Empty<double>();
            boundaryWorldX = Array.Empty<double>();
            return false;
        }

        var externalLeft = root.X;
        var externalRight = root.X + root.Width;

        var boundaries = new List<double> { externalLeft };
        boundaries.AddRange(axes);
        boundaries.Add(externalRight);

        var widths = new List<double>();
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            widths.Add(CadMath.RoundMm(boundaries[i + 1] - boundaries[i]));
        }

        segmentWidthsMm = widths;
        boundaryWorldX = boundaries;
        return true;
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

    private static double WorldXToScreenX(CadEditorViewModel editor, double worldX, double worldBottomY)
    {
        return ToScreenPoint(editor, worldX, worldBottomY).X;
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
        var targetX = centerOnDimLine.X - 5;
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
        var midPoint = new Point(centerOnDimLine.X - 5, centerOnDimLine.Y);

        using (context.PushTransform(Matrix.CreateTranslation(midPoint.X, midPoint.Y)))
        {
            using (context.PushTransform(Matrix.CreateRotation(-Math.PI / 2)))
            {
                context.DrawText(
                    formatted,
                    new Point(-formatted.Width / 2, -formatted.Height / 2));
            }
        }
    }

    private static void DrawCenteredText(DrawingContext context, string text, Point center, IBrush brush)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            DimensionLabelFontSize,
            brush);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
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

    private static VerticalImpostTarget? TryResolveVerticalImpostForSegment(
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

    private static VerticalImpostTarget? FindVerticalImpostAtAxis(FrameNode root, double axisWorldX)
    {
        const double tolerance = 1.5;
        VerticalImpostTarget? match = null;
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
                    match = new VerticalImpostTarget
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
            match = new VerticalImpostTarget { SplitNode = node };
        }

        return match;
    }

    private static void DrawRoundDummyFrame(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawEllipse(null, DashedPen, rect.Center, rect.Width / 2, rect.Height / 2);
    }

    private static void DrawArchFrame(DrawingContext context, CadEditorViewModel editor, GeometryElement element, bool dashed = false)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var start = ToScreenPoint(editor, originX, originY);
        var end = ToScreenPoint(editor, originX + width, originY);
        var radius = Math.Abs(end.X - start.X) / 2;
        if (radius <= 0)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, isFilled: false);
            ctx.LineTo(end);
            ctx.ArcTo(
                start,
                new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: false,
                SweepDirection.CounterClockwise);
            ctx.EndFigure(true);
        }

        context.DrawGeometry(null, dashed ? DashedPen : FramePen, geometry);
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
        var rect = ToScreenRect(editor, element);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(rect.Left, rect.Bottom), isFilled: false);
            ctx.LineTo(new Point(rect.Right, rect.Bottom));
            ctx.LineTo(new Point(rect.Center.X, rect.Top));
            ctx.EndFigure(true);
        }

        context.DrawGeometry(null, DashedPen, geometry);
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
        if (opening is SashOpeningType.Turn or SashOpeningType.TiltTurn)
        {
            var hingeMid = new Point(inner.Left, inner.Center.Y);
            context.DrawLine(DashedAccentPen, hingeMid, inner.TopRight);
            context.DrawLine(DashedAccentPen, hingeMid, inner.BottomRight);
        }

        if (opening == SashOpeningType.TiltTurn)
        {
            var bottomMid = new Point(inner.Center.X, inner.Bottom);
            context.DrawLine(DashedAccentPen, bottomMid, inner.TopLeft);
            context.DrawLine(DashedAccentPen, bottomMid, inner.TopRight);
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

    private static void DrawGlazing(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 2 || rect.Height <= 2)
        {
            return;
        }

        context.DrawRectangle(GlazingFill, null, rect);

        var inset = Math.Min(GlazingInnerInsetPx, Math.Min(rect.Width, rect.Height) / 4.0);
        var inner = new Rect(
            rect.X + inset,
            rect.Y + inset,
            Math.Max(0, rect.Width - inset * 2),
            Math.Max(0, rect.Height - inset * 2));
        if (inner.Width <= 1 || inner.Height <= 1)
        {
            return;
        }

        context.DrawRectangle(null, GlazingBeadPen, inner);
        DrawGlazingBeadCornerMiters(context, rect, inner);
        DrawGlazingReflections(context, inner);
    }

    /// <summary>Диагональные стыки штапика 45° от угла светового проёма к углу внутреннего контура.</summary>
    private static void DrawGlazingBeadCornerMiters(DrawingContext context, Rect outer, Rect inner)
    {
        context.DrawLine(GlazingBeadPen, new Point(outer.Left, outer.Top), new Point(inner.Left, inner.Top));
        context.DrawLine(GlazingBeadPen, new Point(outer.Right, outer.Top), new Point(inner.Right, inner.Top));
        context.DrawLine(GlazingBeadPen, new Point(outer.Left, outer.Bottom), new Point(inner.Left, inner.Bottom));
        context.DrawLine(GlazingBeadPen, new Point(outer.Right, outer.Bottom), new Point(inner.Right, inner.Bottom));
    }

    private static void DrawGlazingReflections(DrawingContext context, Rect inner)
    {
        var centerX = inner.X + inner.Width / 2;
        var centerY = inner.Y + inner.Height / 2;
        var span = Math.Min(inner.Width, inner.Height);
        var centerLength = Math.Clamp(span * 0.55, 28, 84);
        var sideLength = Math.Clamp(span * 0.40, 20, 56);
        var spacing = Math.Clamp(span * 0.12, 8, 14);

        const double angle = Math.PI / 4;
        var dirX = Math.Cos(angle);
        var dirY = -Math.Sin(angle);
        var perpX = -dirY;
        var perpY = dirX;

        DrawGlazingReflectionLine(context, centerX, centerY, perpX, perpY, -spacing, sideLength, dirX, dirY);
        DrawGlazingReflectionLine(context, centerX, centerY, perpX, perpY, 0, centerLength, dirX, dirY);
        DrawGlazingReflectionLine(context, centerX, centerY, perpX, perpY, spacing, sideLength, dirX, dirY);
    }

    private static void DrawGlazingReflectionLine(
        DrawingContext context,
        double centerX,
        double centerY,
        double perpX,
        double perpY,
        double perpOffset,
        double length,
        double dirX,
        double dirY)
    {
        var originX = centerX + perpX * perpOffset;
        var originY = centerY + perpY * perpOffset;
        var half = length / 2;
        context.DrawLine(
            GlazingReflectionPen,
            new Point(originX - dirX * half, originY - dirY * half),
            new Point(originX + dirX * half, originY + dirY * half));
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

    private static void DrawBead(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var half = MarkerRadiusPx * 0.75;
        var rect = new Rect(center.X - half, center.Y - half, half * 2, half * 2);
        context.DrawRectangle(AccentFill, AccentPen, rect);
    }

    private static void DrawAccessory(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
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