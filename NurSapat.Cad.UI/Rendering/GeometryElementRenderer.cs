using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
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
    private static readonly IBrush GlazingFill = new SolidColorBrush(Color.FromArgb(30, 0x9C, 0xA3, 0xAF));
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
                    DrawFrameDimensions(context, editor, element);
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
                    DrawFrameDimensions(context, editor, element);
                    break;
                }
            case GeometryElementKind.ArchFrame:
                DrawArchFrame(context, editor, element);
                DrawArchDimensions(context, editor, element);
                break;
            case GeometryElementKind.ArchDummyFrame:
                DrawArchFrame(context, editor, element, dashed: true);
                DrawArchDimensions(context, editor, element);
                break;
            case GeometryElementKind.TriangleDummyFrame:
                DrawTriangleFrame(context, editor, element);
                DrawTriangleDimensions(context, editor, element);
                break;
            case GeometryElementKind.RoundDummyFrame:
                DrawRoundDummyFrame(context, editor, element);
                DrawCircleDimensions(context, editor, element);
                break;
            case GeometryElementKind.Sash:
                break;
            case GeometryElementKind.SlidingSash:
                DrawSlidingSash(context, editor, element);
                break;
            case GeometryElementKind.Glazing:
                context.DrawRectangle(GlazingFill, ThinPen, ToScreenRect(editor, element));
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
                height);
            return;
        }

        DrawScreenDimensions(context, rect, $"{width:0}", $"{height:0}");
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
        DrawScreenDimensions(context, rect, $"{width:0}", $"{height:0}");

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

    public static void DrawScreenDimensions(DrawingContext context, Rect rect, string widthLabel, string heightLabel)
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
        // TODO: Здесь будет накладываться TextBox для интерактивного изменения размера.
        DrawCenteredText(context, widthLabel, new Point(rect.Center.X, yDim + 4), DimensionTextBrush);

        var xDim = rect.Left - dimOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Top), new Point(xDim, rect.Top));
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Bottom), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim, rect.Top), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Top), new Point(xDim + tick, rect.Top));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Bottom), new Point(xDim + tick, rect.Bottom));
        DrawVerticalTextAlongSide(context, heightLabel, new Point(xDim - 3, rect.Center.Y), DimensionTextBrush);
    }

    private static void DrawScreenDimensionsWithSegmentedBottom(
        DrawingContext context,
        CadEditorViewModel editor,
        Rect rect,
        GeometryElement element,
        IReadOnlyList<double> segmentWidthsMm,
        IReadOnlyList<double> boundaryWorldX,
        double totalWidthMm,
        double heightMm)
    {
        DrawLeftHeightDimension(context, rect, heightMm);

        var (_, originY, _, _) = GetWorldOriginBounds(element);
        var screenBoundaries = boundaryWorldX
            .Select(worldX => WorldXToScreenX(editor, worldX, originY))
            .ToList();

        DrawBottomWidthDimensionChain(
            context,
            screenBoundaries,
            segmentWidthsMm,
            totalWidthMm,
            rect.Bottom);
    }

    private static void DrawLeftHeightDimension(DrawingContext context, Rect rect, double heightMm)
    {
        const double dimOffset = DimensionExtensionOffset;
        const double tick = DimensionTickSize;

        var xDim = rect.Left - dimOffset;
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Top), new Point(xDim, rect.Top));
        context.DrawLine(DimensionPen, new Point(rect.Left, rect.Bottom), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim, rect.Top), new Point(xDim, rect.Bottom));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Top), new Point(xDim + tick, rect.Top));
        context.DrawLine(DimensionPen, new Point(xDim - tick, rect.Bottom), new Point(xDim + tick, rect.Bottom));
        DrawVerticalTextAlongSide(context, $"{heightMm:0}", new Point(xDim - 3, rect.Center.Y), DimensionTextBrush);
    }

    private static void DrawBottomWidthDimensionChain(
        DrawingContext context,
        IReadOnlyList<double> screenBoundaries,
        IReadOnlyList<double> segmentWidthsMm,
        double totalWidthMm,
        double outerBottomScreenY)
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

        for (var i = 0; i < segmentWidthsMm.Count; i++)
        {
            var x1 = screenBoundaries[i];
            var x2 = screenBoundaries[i + 1];
            context.DrawLine(DimensionPen, new Point(x1, ySeg), new Point(x2, ySeg));
            // TODO: Здесь будет накладываться TextBox для интерактивного изменения размера.
            DrawCenteredText(
                context,
                $"{segmentWidthsMm[i]:0}",
                new Point((x1 + x2) / 2, ySeg - 4),
                DimensionTextBrush);
        }

        var yOverall = outerBottomScreenY + overallOffset;
        var left = screenBoundaries[0];
        var right = screenBoundaries[^1];
        context.DrawLine(DimensionPen, new Point(left, ySeg + tick), new Point(left, yOverall));
        context.DrawLine(DimensionPen, new Point(right, ySeg + tick), new Point(right, yOverall));
        context.DrawLine(DimensionPen, new Point(left, yOverall), new Point(right, yOverall));
        context.DrawLine(DimensionPen, new Point(left, yOverall - tick), new Point(left, yOverall + tick));
        context.DrawLine(DimensionPen, new Point(right, yOverall - tick), new Point(right, yOverall + tick));
        DrawCenteredText(context, $"{totalWidthMm:0}", new Point((left + right) / 2, yOverall + 4), DimensionTextBrush);
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

    private static void DrawVerticalTextAlongSide(DrawingContext context, string text, Point centerOnDimLine, IBrush brush)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            brush);

        double targetX = centerOnDimLine.X - 5;
        double targetY = centerOnDimLine.Y;

        var transform = Matrix.CreateTranslation(-formatted.Width / 2, -formatted.Height / 2) *
                        Matrix.CreateRotation(-Math.PI / 2) *
                        Matrix.CreateTranslation(targetX, targetY);

        using (context.PushTransform(transform))
        {
            context.DrawText(formatted, new Point(0, 0));
        }
    }

    private static void DrawCenteredText(DrawingContext context, string text, Point center, IBrush brush)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            brush);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
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