using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Отрисовка коробки рамы с объёмным профилем и угловыми стыками под 45°.
/// Координаты: X/Y — нижний левый угол, Y растёт вверх.
/// </summary>
public static class FrameVolumeRenderer
{
    public static readonly Color MiterColor = Color.Parse("#919191");
    public static readonly Color ProfileStrokeColor = Color.Parse("#919191");

    /// <summary>Толщина контура профиля коробки и импоста (px).</summary>
    public const double ProfileStrokeThickness = 1.5;

    private static readonly IBrush InnerOpeningFill = Brushes.White;
    private static readonly IBrush ProfileFill = new SolidColorBrush(Color.FromArgb(48, 0xE5, 0xE5, 0xE5));
    private static readonly IBrush ProfileStrokeBrush = new SolidColorBrush(ProfileStrokeColor);
    private static readonly IBrush MiterBrush = new SolidColorBrush(MiterColor);
    public static readonly IPen ProfilePen = new Pen(ProfileStrokeBrush, ProfileStrokeThickness);
    private static readonly IPen MiterPen = new Pen(MiterBrush, 1.2);
    public static readonly IPen DashedProfilePen = new Pen(
        ProfileStrokeBrush,
        ProfileStrokeThickness,
        new DashStyle(new double[] { 4, 3 }, 0));

    public static IPen ResolveProfileOutlinePen(bool dashed) =>
        dashed ? DashedProfilePen : ProfilePen;

    public static void Draw(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        bool dashed = false,
        bool opaqueProfile = false)
    {
        if (editor.Viewport == null || width <= 0 || height <= 0)
        {
            return;
        }

        var profileFill = opaqueProfile ? InnerOpeningFill : ProfileFill;

        if (opaqueProfile)
        {
            var outer = WorldBoundsToScreenRect(editor, originX, originY, width, height);
            if (outer.Width > 0 && outer.Height > 0)
            {
                context.FillRectangle(InnerOpeningFill, outer);
            }
        }

        var t = ResolveProfileThickness(width, height, profileThickness);
        t = t / 2.0;
        if (t <= CadMath.Epsilon)
        {
            DrawFallbackOutline(context, editor, originX, originY, width, height, dashed);
            return;
        }

        var x0 = originX;
        var y0 = originY;
        var x1 = originX + width;
        var y1 = originY + height;

        var xi0 = x0 + t;
        var yi0 = y0 + t;
        var xi1 = x1 - t;
        var yi1 = y1 - t;

        FillInnerOpening(context, editor, xi0, yi0, xi1, yi1);

        var outlinePen = dashed ? DashedProfilePen : ProfilePen;

        DrawRail(context, editor, outlinePen, profileFill,
            new CadPoint(x0, y0), new CadPoint(x1, y0),
            new CadPoint(xi1, yi0), new CadPoint(xi0, yi0));

        DrawRail(context, editor, outlinePen, profileFill,
            new CadPoint(x0, y1), new CadPoint(x1, y1),
            new CadPoint(xi1, yi1), new CadPoint(xi0, yi1));

        DrawRail(context, editor, outlinePen, profileFill,
            new CadPoint(x0, y0), new CadPoint(x0, y1),
            new CadPoint(xi0, yi1), new CadPoint(xi0, yi0));

        DrawRail(context, editor, outlinePen, profileFill,
            new CadPoint(x1, y0), new CadPoint(x1, y1),
            new CadPoint(xi1, yi1), new CadPoint(xi1, yi0));

        DrawCornerMiter(context, editor, new CadPoint(x0, y0), new CadPoint(xi0, yi0));
        DrawCornerMiter(context, editor, new CadPoint(x1, y0), new CadPoint(xi1, yi0));
        DrawCornerMiter(context, editor, new CadPoint(x0, y1), new CadPoint(xi0, yi1));
        DrawCornerMiter(context, editor, new CadPoint(x1, y1), new CadPoint(xi1, yi1));
    }

    public static (double X, double Y, double Width, double Height) GetInnerOpeningWorldBounds(
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness)
    {
        var inset = ResolveProfileInset(width, height, profileThickness);
        return (originX + inset, originY + inset, width - inset * 2.0, height - inset * 2.0);
    }

    public static double ResolveProfileInset(double width, double height, double profileThickness) =>
        ResolveProfileThickness(width, height, profileThickness) / 2.0;

    public static Rect ToInnerOpeningScreenRect(
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness)
    {
        var (xi0, yi0, wi, hi) = GetInnerOpeningWorldBounds(originX, originY, width, height, profileThickness);
        return WorldBoundsToScreenRect(editor, xi0, yi0, wi, hi);
    }

    private static double ResolveProfileThickness(double width, double height, double profileThickness)
    {
        if (profileThickness <= CadMath.Epsilon)
        {
            return 0;
        }

        var maxT = Math.Min(width, height) * 0.45;

        // Защита от ArgumentException в Math.Clamp
        if (maxT <= 2)
        {
            return maxT;
        }

        return Math.Clamp(profileThickness, 2, maxT);
    }

    private static void FillInnerOpening(
        DrawingContext context,
        CadEditorViewModel editor,
        double xi0,
        double yi0,
        double xi1,
        double yi1)
    {
        if (xi1 <= xi0 || yi1 <= yi0)
        {
            return;
        }

        var inner = WorldBoundsToScreenRect(editor, xi0, yi0, xi1 - xi0, yi1 - yi0);
        if (inner.Width > 0 && inner.Height > 0)
        {
            context.FillRectangle(InnerOpeningFill, inner);
        }
    }

    private static void DrawRail(
        DrawingContext context,
        CadEditorViewModel editor,
        IPen pen,
        IBrush fill,
        CadPoint outerA,
        CadPoint outerB,
        CadPoint innerB,
        CadPoint innerA)
    {
        var pOuterA = WorldToScreen(editor, outerA);
        var pOuterB = WorldToScreen(editor, outerB);
        var pInnerB = WorldToScreen(editor, innerB);
        var pInnerA = WorldToScreen(editor, innerA);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(pOuterA, isFilled: true);
            ctx.LineTo(pOuterB);
            ctx.LineTo(pInnerB);
            ctx.LineTo(pInnerA);
            ctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static void DrawCornerMiter(DrawingContext context, CadEditorViewModel editor, CadPoint a, CadPoint b)
    {
        DrawWorldLine(context, editor, MiterPen, a, b);
    }

    private static void DrawFallbackOutline(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        bool dashed)
    {
        var rect = WorldBoundsToScreenRect(editor, originX, originY, width, height);
        var pen = dashed ? DashedProfilePen : ProfilePen;
        context.DrawRectangle(null, pen, rect);
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint point)
    {
        var p = editor.Viewport!.WorldToScreen(point);
        return new Point(p.X, p.Y);
    }

    private static void DrawWorldLine(
        DrawingContext context,
        CadEditorViewModel editor,
        IPen pen,
        CadPoint start,
        CadPoint end)
    {
        var p1 = WorldToScreen(editor, start);
        var p2 = WorldToScreen(editor, end);
        context.DrawLine(pen, p1, p2);
    }

    private static Rect WorldBoundsToScreenRect(
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height)
    {
        var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(originX, originY + height));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(originX + width, originY));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }
}