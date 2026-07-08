using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Отрисовка гофрированного заполнения проёма с обрезкой по контуру.</summary>
internal static class CorrugationFillRenderer
{
    private const double LineSpacingMm = 25;

    private static readonly IPen CorrugationPen = new Pen(
        new SolidColorBrush(Color.Parse("#8B7355")),
        1.2);

    private static readonly IBrush CorrugationFill = new SolidColorBrush(Color.FromArgb(40, 0xC4, 0xA8, 0x7C));

    public static void DrawCorrugationElement(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement element,
        IReadOnlyList<GeometryElement> allElements)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var fillType = element.FillType ?? ResolveFillTypeFromSource(element.SourceToolId);
        if (fillType == GlazingType.Glass)
        {
            return;
        }

        if (TryResolveCorrugationBounds(editor, element, allElements, out var bounds))
        {
            DrawCorrugationInBounds(context, editor, bounds, fillType);
        }
    }

    private static bool TryResolveCorrugationBounds(
        CadEditorViewModel editor,
        GeometryElement element,
        IReadOnlyList<GeometryElement> allElements,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        if (element.Kind == GeometryElementKind.Glazing
            && SashGeometry.TryResolveGlassForGlazingElement(editor, element, allElements, out bounds))
        {
            return bounds.Width > 2 && bounds.Height > 2;
        }

        if (element.Kind == GeometryElementKind.Corrugation)
        {
            var hostGlazing = allElements.FirstOrDefault(
                e => e.Kind == GeometryElementKind.Glazing
                     && (e.HostSashId == element.HostSashId || ContainsSameLeaf(editor, e, element)));

            if (hostGlazing is not null
                && SashGeometry.TryResolveGlassForGlazingElement(editor, hostGlazing, allElements, out bounds))
            {
                return bounds.Width > 2 && bounds.Height > 2;
            }

            var cx = (double)element.X;
            var cy = (double)element.Y;
            var halfW = (double)element.WidthMm / 2.0;
            var halfH = (double)element.HeightMm / 2.0;
            bounds = SashGeometry.GlassOpeningBounds.FromRect(
                cx - halfW,
                cy - halfH,
                cx + halfW,
                cy + halfH);
            return bounds.Width > 2 && bounds.Height > 2;
        }

        return false;
    }

    private static bool ContainsSameLeaf(CadEditorViewModel editor, GeometryElement a, GeometryElement b)
    {
        if (editor.FrameRoot is null)
        {
            return false;
        }

        var leafA = editor.FrameRoot.FindLeafAt((double)a.X, (double)a.Y);
        var leafB = editor.FrameRoot.FindLeafAt((double)b.X, (double)b.Y);
        return leafA is not null && ReferenceEquals(leafA, leafB);
    }

    public static GlazingType ResolveFillTypeFromSource(string? sourceToolId) =>
        sourceToolId switch
        {
            "corrugation-h" => GlazingType.HorizontalCorrugation,
            "corrugation-v" => GlazingType.VerticalCorrugation,
            _ => GlazingType.VerticalCorrugation
        };

    public static void DrawCorrugationInBounds(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds bounds,
        GlazingType fillType)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        var loop = bounds.BuildOuterContourLoop();
        FillGlazingContour(context, editor, loop, CorrugationFill);

        if (bounds.IsArched)
        {
            DrawParallelLines(context, editor, bounds, fillType);
        }
        else
        {
            var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(bounds.Left, bounds.Top));
            var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(bounds.Right, bounds.Bottom));
            var rect = new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
            using (context.PushClip(rect))
            {
                DrawParallelLines(context, editor, bounds, fillType);
            }
        }

        StrokeGlazingContour(context, editor, loop);
    }

    private static void DrawParallelLines(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds bounds,
        GlazingType fillType)
    {
        if (fillType == GlazingType.VerticalCorrugation)
        {
            for (var x = bounds.Left + LineSpacingMm; x < bounds.Right; x += LineSpacingMm)
            {
                DrawClippedVerticalLine(context, editor, bounds, x);
            }

            return;
        }

        for (var y = bounds.Bottom + LineSpacingMm; y < bounds.Top; y += LineSpacingMm)
        {
            DrawClippedHorizontalLine(context, editor, bounds, y);
        }
    }

    private static void DrawClippedVerticalLine(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds bounds,
        double x)
    {
        var bottom = bounds.Bottom;
        var top = bounds.ResolveTopAtX(x);
        if (top - bottom > 1)
        {
            DrawWorldLine(context, editor, new CadPoint(x, bottom), new CadPoint(x, top));
        }
    }

    private static void DrawClippedHorizontalLine(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds bounds,
        double y)
    {
        if (bounds.IsArched && y > bounds.SpringingLeft.Y + CadMath.Epsilon)
        {
            if (!TryResolveArchedHorizontalSpan(bounds, y, out var left, out var right))
            {
                return;
            }

            DrawWorldLine(context, editor, new CadPoint(left, y), new CadPoint(right, y));
            return;
        }

        DrawWorldLine(context, editor, new CadPoint(bounds.Left, y), new CadPoint(bounds.Right, y));
    }

    private static bool TryResolveArchedHorizontalSpan(
        SashGeometry.GlassOpeningBounds bounds,
        double y,
        out double left,
        out double right)
    {
        left = 0;
        right = 0;
        if (y > bounds.ArcCenter.Y + bounds.ArcRadius)
        {
            return false;
        }

        var dy = y - bounds.ArcCenter.Y;
        var underRoot = bounds.ArcRadius * bounds.ArcRadius - dy * dy;
        if (underRoot < 0)
        {
            return false;
        }

        var halfSpan = Math.Sqrt(underRoot);
        left = bounds.ArcCenter.X - halfSpan;
        right = bounds.ArcCenter.X + halfSpan;
        return right > left;
    }

    private static void DrawWorldLine(
        DrawingContext context,
        CadEditorViewModel editor,
        CadPoint start,
        CadPoint end)
    {
        var p1 = editor.Viewport!.WorldToScreen(start);
        var p2 = editor.Viewport.WorldToScreen(end);
        context.DrawLine(CorrugationPen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
    }

    private static void FillGlazingContour(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IBrush fill)
    {
        var points = loop.Tessellate()
            .Select(p =>
            {
                var s = editor.Viewport!.WorldToScreen(p);
                return new Point(s.X, s.Y);
            })
            .ToList();

        if (points.Count < 3)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], isFilled: true);
            for (var i = 1; i < points.Count; i++)
            {
                ctx.LineTo(points[i]);
            }

            ctx.EndFigure(true);
        }

        context.DrawGeometry(fill, null, geometry);
    }

    private static void StrokeGlazingContour(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop)
    {
        foreach (var edge in loop.Edges)
        {
            if (edge.Kind == ProfileEdgeKind.Line)
            {
                var start = editor.Viewport!.WorldToScreen(edge.Start);
                var end = editor.Viewport.WorldToScreen(edge.End);
                context.DrawLine(CorrugationPen, new Point(start.X, start.Y), new Point(end.X, end.Y));
                continue;
            }

            var previousScreen = editor.Viewport!.WorldToScreen(edge.Start);
            var previous = new Point(previousScreen.X, previousScreen.Y);
            foreach (var worldPoint in CircularArc.TessellateSpringingArc(
                         edge.ArcCenter,
                         edge.ArcRadius,
                         edge.ArcStartAngleRad,
                         edge.ArcEndAngleRad))
            {
                var currentScreen = editor.Viewport.WorldToScreen(worldPoint);
                var current = new Point(currentScreen.X, currentScreen.Y);
                context.DrawLine(CorrugationPen, previous, current);
                previous = current;
            }
        }
    }
}
