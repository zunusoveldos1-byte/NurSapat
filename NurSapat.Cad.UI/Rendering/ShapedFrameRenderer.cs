using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Объёмная отрисовка круглых и треугольных коробок.</summary>
public static class ShapedFrameRenderer
{
    private static readonly IBrush ProfileFill = new SolidColorBrush(Color.FromArgb(48, 0xE5, 0xE5, 0xE5));

    public static void DrawShapedFrame(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        ConstructionShape shape,
        bool dashed = false)
    {
        switch (shape)
        {
            case ConstructionShape.Circle:
                DrawCircularFrame(context, editor, originX, originY, width, height, profileThickness, dashed);
                break;
            case ConstructionShape.Triangle:
                DrawTriangularFrame(context, editor, originX, originY, width, height, profileThickness, dashed);
                break;
        }
    }

    private static void DrawCircularFrame(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        bool dashed)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var diameter = Math.Min(width, height);
        var centerX = originX + width / 2.0;
        var centerY = originY + height / 2.0;
        var outerRadius = diameter / 2.0;
        var thickness = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThickness);
        var innerRadius = Math.Max(outerRadius - thickness, outerRadius * 0.25);
        var pen = FrameVolumeRenderer.ResolveProfileOutlinePen(dashed);

        var outerRect = WorldCircleToScreenRect(editor, centerX, centerY, outerRadius);
        var innerRect = WorldCircleToScreenRect(editor, centerX, centerY, innerRadius);

        context.DrawEllipse(ProfileFill, null, outerRect.Center, outerRect.Width / 2, outerRect.Height / 2);
        context.DrawEllipse(Brushes.White, null, innerRect.Center, innerRect.Width / 2, innerRect.Height / 2);
        context.DrawEllipse(null, pen, outerRect.Center, outerRect.Width / 2, outerRect.Height / 2);
        context.DrawEllipse(null, pen, innerRect.Center, innerRect.Width / 2, innerRect.Height / 2);
    }

    public static void DrawTriangularOutline(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        IPen pen)
    {
        if (editor.Viewport is null || width <= 0 || height <= 0)
        {
            return;
        }

        var outer = FrameTriangleGeometry.BuildOuterTriangle(originX, originY, width, height);
        void Stroke(CadPoint a, CadPoint b)
        {
            var p1 = ToScreen(editor, a);
            var p2 = ToScreen(editor, b);
            context.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
        }

        Stroke(outer.Bl, outer.Br);
        Stroke(outer.Br, outer.Top);
        Stroke(outer.Top, outer.Bl);
    }

    private static void DrawTriangularFrame(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        bool dashed)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var pen = FrameVolumeRenderer.ResolveProfileOutlinePen(dashed);
        var outer = BuildTrianglePoints(originX, originY, width, height);
        var thickness = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThickness);
        var inner = InsetTriangle(outer, thickness);

        FillTriangleBand(context, editor, outer, inner);
        DrawTriangleBand(context, editor, outer, inner, pen);
    }

    private static (CadPoint Bl, CadPoint Br, CadPoint Top) BuildTrianglePoints(
        double originX,
        double originY,
        double width,
        double height)
    {
        var bl = new CadPoint(originX, originY);
        var br = new CadPoint(originX + width, originY);
        var top = new CadPoint(originX + width / 2.0, originY + height);
        return (bl, br, top);
    }

    private static (CadPoint Bl, CadPoint Br, CadPoint Top) InsetTriangle(
        (CadPoint Bl, CadPoint Br, CadPoint Top) outer,
        double inset)
    {
        if (inset <= CadMath.Epsilon)
        {
            return outer;
        }

        var centroid = new CadPoint(
            (outer.Bl.X + outer.Br.X + outer.Top.X) / 3.0,
            (outer.Bl.Y + outer.Br.Y + outer.Top.Y) / 3.0);

        return (
            InsetToward(outer.Bl, centroid, inset),
            InsetToward(outer.Br, centroid, inset),
            InsetToward(outer.Top, centroid, inset));
    }

    private static CadPoint InsetToward(CadPoint point, CadPoint target, double distance)
    {
        var dx = target.X - point.X;
        var dy = target.Y - point.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= CadMath.Epsilon)
        {
            return point;
        }

        var scale = Math.Min(distance / length, 0.45);
        return new CadPoint(point.X + dx * scale, point.Y + dy * scale);
    }

    private static void FillTriangleBand(
        DrawingContext context,
        CadEditorViewModel editor,
        (CadPoint Bl, CadPoint Br, CadPoint Top) outer,
        (CadPoint Bl, CadPoint Br, CadPoint Top) inner)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var p0 = ToScreen(editor, outer.Bl);
            var p1 = ToScreen(editor, outer.Br);
            var p2 = ToScreen(editor, outer.Top);
            var i0 = ToScreen(editor, inner.Bl);
            var i1 = ToScreen(editor, inner.Br);
            var i2 = ToScreen(editor, inner.Top);

            ctx.BeginFigure(p0, isFilled: true);
            ctx.LineTo(p1);
            ctx.LineTo(p2);
            ctx.LineTo(i2);
            ctx.LineTo(i1);
            ctx.LineTo(i0);
            ctx.EndFigure(true);
        }

        context.DrawGeometry(ProfileFill, null, geometry);
    }

    private static void DrawTriangleBand(
        DrawingContext context,
        CadEditorViewModel editor,
        (CadPoint Bl, CadPoint Br, CadPoint Top) outer,
        (CadPoint Bl, CadPoint Br, CadPoint Top) inner,
        IPen pen)
    {
        void Stroke(CadPoint a, CadPoint b)
        {
            var p1 = ToScreen(editor, a);
            var p2 = ToScreen(editor, b);
            context.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
        }

        Stroke(outer.Bl, outer.Br);
        Stroke(outer.Br, outer.Top);
        Stroke(outer.Top, outer.Bl);
        Stroke(inner.Bl, inner.Br);
        Stroke(inner.Br, inner.Top);
        Stroke(inner.Top, inner.Bl);
        Stroke(outer.Bl, inner.Bl);
        Stroke(outer.Br, inner.Br);
        Stroke(outer.Top, inner.Top);
    }

    private static Point ToScreen(CadEditorViewModel editor, CadPoint point)
    {
        var screen = editor.Viewport!.WorldToScreen(point);
        return new Point(screen.X, screen.Y);
    }

    private static Rect WorldCircleToScreenRect(CadEditorViewModel editor, double cx, double cy, double radius)
    {
        var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(cx - radius, cy + radius));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(cx + radius, cy - radius));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }

    public static double ResolveTrianglePerimeterMm(double width, double height)
    {
        var halfBase = width / 2.0;
        var side = Math.Sqrt(halfBase * halfBase + height * height);
        return width + side * 2;
    }
}
