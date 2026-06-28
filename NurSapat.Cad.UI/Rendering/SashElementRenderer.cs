using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class SashElementRenderer
{
    private static readonly IBrush OpeningLineBrush = new SolidColorBrush(Color.Parse("#696969"));
    private static readonly IPen HardwareOpeningPen = new Pen(
        OpeningLineBrush,
        1.0,
        new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen PreviewPen = new Pen(new SolidColorBrush(Color.Parse("#0078D7")), 1, new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen HingePen = new Pen(new SolidColorBrush(Color.Parse("#969696")), 2.0);
    private static readonly IBrush HandleBackgroundBrush = Brushes.White;
    private static readonly IBrush HandlePreviewFillBrush = new SolidColorBrush(Color.FromArgb(48, 0, 120, 215));
    private static readonly IPen HandleOutlinePen = new Pen(new SolidColorBrush(Color.Parse("#969696")), 1.8);

    /// <summary>Ширина розетки вдоль профиля створки (мм).</summary>
    private const double HandleBaseWidthMm = 24.0;
    /// <summary>Высота розетки вдоль створки (мм).</summary>
    private const double HandleBaseHeightMm = 65.0;
    /// <summary>Толщина рукоятки (мм).</summary>
    private const double HandleLeverThicknessMm = 16.0;
    /// <summary>Длина рукоятки в сторону стекла (мм).</summary>
    private const double HandleLeverLengthMm = 110.0;
    private const double HandleGlassInsetMm = 4.0;

    /// <summary>Ширина петли вдоль профиля створки (мм).</summary>
    private const double HingeWidthMm = 19.0;
    /// <summary>Высота петли вдоль створки / глубина в раму (мм).</summary>
    private const double HingeHeightMm = 65.0;
    /// <summary>Технологический отступ петли от края створки (мм).</summary>
    private const double HingeEdgeOffsetMm = 60.0;
    /// <summary>Нахлёст петли на раму за внешним контуром створки (мм).</summary>
    private const double HingeFrameOverlapMm = 6.0;
    private static readonly IBrush HingeFillBrush = Brushes.White;

    public static void DrawAll(DrawingContext context, CadEditorViewModel editor, IEnumerable<GeometryElement> elements)
    {
        foreach (var element in elements)
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                Draw(context, editor, element);
            }
        }
    }

    public static void Draw(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        var inner = SashGeometry.TryResolveInnerBounds(editor, element, out var liveInner)
            ? liveInner
            : SashGeometry.InnerOpeningBoundsFromElement(element);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        var hinge = element.HingeSide ?? SashHingeSide.Left;
        var opening = element.OpeningType ?? SashOpeningType.TiltTurn;
        DrawSashBody(
            context,
            editor,
            inner,
            hinge,
            opening,
            element.HasHardware,
            element.HasHandle,
            preview: false);
    }

    public static void DrawPreview(DrawingContext context, CadEditorViewModel editor, SashDragPreview preview)
    {
        if (editor?.Viewport == null || preview.Width <= 0 || preview.Height <= 0)
        {
            return;
        }

        DrawSashBody(
            context,
            editor,
            new SashGeometry.InnerOpeningBounds
            {
                Left = preview.OriginX,
                Bottom = preview.OriginY,
                Right = preview.OriginX + preview.Width,
                Top = preview.OriginY + preview.Height
            },
            preview.HingeSide,
            SashOpeningType.TiltTurn,
            hasHardware: false,
            hasHandle: false,
            preview: true);
    }

    public static void DrawHandleMarker(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement sash,
        SashHingeSide hingeSide,
        bool preview)
    {
        if (editor?.Viewport == null)
        {
            return;
        }

        var (glassLeft, glassBottom, glassRight, glassTop, outer) = ResolveHandleWorldBounds(editor, sash);
        if (glassRight <= glassLeft || glassTop <= glassBottom)
        {
            return;
        }

        DrawHandleOnGlass(
            context,
            editor,
            glassLeft,
            glassBottom,
            glassRight,
            glassTop,
            outer,
            hingeSide,
            preview);
    }

    private static void DrawSashBody(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashHingeSide hingeSide,
        SashOpeningType openingType,
        bool hasHardware,
        bool hasHandle,
        bool preview)
    {
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return;
        }

        FrameVolumeRenderer.Draw(
            context,
            editor,
            outer.Left,
            outer.Bottom,
            outer.Width,
            outer.Height,
            ResolveSashDrawProfileThickness(),
            dashed: preview,
            opaqueProfile: !preview);

        var (glassLeft, glassBottom, glassRight, glassTop) = SashGeometry.ResolveInnerGlassFromOuter(
            outer.Left,
            outer.Bottom,
            outer.Width,
            outer.Height);
        if (glassRight <= glassLeft || glassTop <= glassBottom)
        {
            return;
        }

        if (hasHardware && !preview)
        {
            DrawOpeningTriangle(
                context,
                editor,
                glassLeft,
                glassBottom,
                glassRight,
                glassTop,
                hingeSide,
                HardwareOpeningPen);
        }

        if (hasHandle && glassRight > glassLeft && glassTop > glassBottom)
        {
            DrawHandleOnGlass(
                context,
                editor,
                glassLeft,
                glassBottom,
                glassRight,
                glassTop,
                outer,
                hingeSide,
                preview);
        }

        if (hasHardware && !preview)
        {
            DrawSashHinges(context, editor, outer, hingeSide, openingType, preview);
        }
    }

    /// <summary>
    /// FrameVolumeRenderer делит толщину пополам для профиля — передаём удвоенное значение, чтобы inset = 60 мм.
    /// </summary>
    private static double ResolveSashDrawProfileThickness() => SashGeometry.ProfileThicknessMm * 2.0;

    private static (double Left, double Bottom, double Right, double Top, SashGeometry.OuterBounds Outer)
        ResolveHandleWorldBounds(CadEditorViewModel editor, GeometryElement element)
    {
        var inner = SashGeometry.TryResolveInnerBounds(editor, element, out var liveInner)
            ? liveInner
            : SashGeometry.InnerOpeningBoundsFromElement(element);
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        var (glassLeft, glassBottom, glassRight, glassTop) = SashGeometry.ResolveInnerGlassFromOuter(
            outer.Left,
            outer.Bottom,
            outer.Width,
            outer.Height);
        return (glassLeft, glassBottom, glassRight, glassTop, outer);
    }

    private static void DrawOpeningTriangle(
        DrawingContext context,
        CadEditorViewModel editor,
        double glassLeft,
        double glassBottom,
        double glassRight,
        double glassTop,
        SashHingeSide hingeSide,
        IPen pen)
    {
        var apex = GetApexWorldPoint(glassLeft, glassBottom, glassRight, glassTop, hingeSide);
        var (hingeA, hingeB) = GetHingeCornerWorldPoints(glassLeft, glassBottom, glassRight, glassTop, hingeSide);

        var pApex = WorldToScreen(editor, apex);
        var pHingeA = WorldToScreen(editor, hingeA);
        var pHingeB = WorldToScreen(editor, hingeB);

        context.DrawLine(pen, pHingeA, pApex);
        context.DrawLine(pen, pHingeB, pApex);
    }

    private static void DrawSashHinges(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.OuterBounds outer,
        SashHingeSide hingeSide,
        SashOpeningType openingType,
        bool preview)
    {
        var pen = preview ? PreviewPen : HingePen;
        var effectiveSide = ResolveHingeMountSide(hingeSide, openingType);

        switch (effectiveSide)
        {
            case SashHingeSide.Left:
            case SashHingeSide.Right:
                DrawVerticalSashHinges(context, editor, outer, effectiveSide, pen);
                break;
            case SashHingeSide.Top:
                DrawHorizontalSashHinges(context, editor, outer, mountOnTop: true, pen);
                break;
            case SashHingeSide.Bottom:
                DrawHorizontalSashHinges(context, editor, outer, mountOnTop: false, pen);
                break;
        }
    }

    /// <summary>
    /// Петли на стороне HingeSide (противоположной ручке — куда сходятся линии открывания).
    /// </summary>
    private static SashHingeSide ResolveHingeMountSide(SashHingeSide hingeSide, SashOpeningType _) => hingeSide;

    private static void DrawVerticalSashHinges(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.OuterBounds outer,
        SashHingeSide hingeSide,
        IPen pen)
    {
        var offset = ResolveHingeEdgeOffset(outer.Height);
        var hingeHeight = Math.Min(HingeHeightMm, Math.Max(HingeHeightMm * 0.85, outer.Height - offset * 2 - 10));
        if (hingeHeight <= CadMath.Epsilon)
        {
            return;
        }

        var topHingeBottom = outer.Top - offset;
        var topHingeTop = topHingeBottom - hingeHeight;
        var bottomHingeBottom = outer.Bottom + offset;
        var bottomHingeTop = bottomHingeBottom + hingeHeight;

        if (hingeSide == SashHingeSide.Right)
        {
            var innerX = outer.Right - HingeWidthMm;
            var outerX = outer.Right + HingeFrameOverlapMm;
            DrawWorldHingeRect(context, editor, innerX, topHingeTop, outerX, topHingeBottom, vertical: true, pen);
            DrawWorldHingeRect(context, editor, innerX, bottomHingeBottom, outerX, bottomHingeTop, vertical: true, pen);
        }
        else
        {
            var innerX = outer.Left + HingeWidthMm;
            var outerX = outer.Left - HingeFrameOverlapMm;
            DrawWorldHingeRect(context, editor, outerX, topHingeTop, innerX, topHingeBottom, vertical: true, pen);
            DrawWorldHingeRect(context, editor, outerX, bottomHingeBottom, innerX, bottomHingeTop, vertical: true, pen);
        }
    }

    private static void DrawHorizontalSashHinges(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.OuterBounds outer,
        bool mountOnTop,
        IPen pen)
    {
        var hingeAlongEdge = HingeWidthMm;
        var hingeIntoFrame = HingeHeightMm;
        if (hingeAlongEdge <= CadMath.Epsilon)
        {
            return;
        }

        var centerX = (outer.Left + outer.Right) / 2.0;
        var gap = 16.0;
        var leftInner = centerX - gap - hingeAlongEdge;
        var leftOuter = centerX - gap;
        var rightInner = centerX + gap;
        var rightOuter = centerX + gap + hingeAlongEdge;

        if (mountOnTop)
        {
            var innerY = outer.Top - HingeWidthMm;
            var outerY = outer.Top + hingeIntoFrame;
            DrawWorldHingeRect(context, editor, leftInner, innerY, leftOuter, outerY, vertical: false, pen);
            DrawWorldHingeRect(context, editor, rightInner, innerY, rightOuter, outerY, vertical: false, pen);
        }
        else
        {
            var innerY = outer.Bottom + HingeWidthMm;
            var outerY = outer.Bottom - hingeIntoFrame;
            DrawWorldHingeRect(context, editor, leftInner, outerY, leftOuter, innerY, vertical: false, pen);
            DrawWorldHingeRect(context, editor, rightInner, outerY, rightOuter, innerY, vertical: false, pen);
        }
    }

    private static double ResolveHingeEdgeOffset(double spanMm) =>
        Math.Min(HingeEdgeOffsetMm, Math.Max(30, spanMm * 0.12));

    private static void DrawWorldHingeRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double bottom,
        double right,
        double top,
        bool vertical,
        IPen pen)
    {
        if (right - left <= CadMath.Epsilon || top - bottom <= CadMath.Epsilon)
        {
            return;
        }

        var rect = WorldBoundsToScreenRect(editor, left, bottom, right - left, top - bottom);
        context.FillRectangle(HingeFillBrush, rect);
        context.DrawRectangle(null, pen, rect);

        var divisionPen = new Pen(pen.Brush, 1.0);
        if (vertical)
        {
            var splitX = rect.Left + rect.Width * 0.35;
            context.DrawLine(divisionPen, new Point(splitX, rect.Top), new Point(splitX, rect.Bottom));
        }
        else
        {
            var splitY = rect.Top + rect.Height * 0.35;
            context.DrawLine(divisionPen, new Point(rect.Left, splitY), new Point(rect.Right, splitY));
        }
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

    private static void DrawHandleOnGlass(
        DrawingContext context,
        CadEditorViewModel editor,
        double glassLeft,
        double glassBottom,
        double glassRight,
        double glassTop,
        SashGeometry.OuterBounds outer,
        SashHingeSide hingeSide,
        bool preview)
    {
        if (glassRight <= glassLeft || glassTop <= glassBottom)
        {
            return;
        }

        var fill = preview ? HandlePreviewFillBrush : HandleBackgroundBrush;
        var pen = preview ? PreviewPen : HandleOutlinePen;

        switch (hingeSide)
        {
            case SashHingeSide.Right:
                DrawVerticalSashHandle(context, editor, outer, glassLeft, glassRight, glassBottom, glassTop, handleOnLeft: true, fill, pen);
                break;
            case SashHingeSide.Left:
                DrawVerticalSashHandle(context, editor, outer, glassLeft, glassRight, glassBottom, glassTop, handleOnLeft: false, fill, pen);
                break;
            case SashHingeSide.Top:
                DrawHorizontalSashHandle(context, editor, outer, glassLeft, glassRight, glassBottom, glassTop, handleOnBottom: true, fill, pen);
                break;
            case SashHingeSide.Bottom:
                DrawHorizontalSashHandle(context, editor, outer, glassLeft, glassRight, glassBottom, glassTop, handleOnBottom: false, fill, pen);
                break;
        }
    }

    private static void DrawVerticalSashHandle(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.OuterBounds outer,
        double glassLeft,
        double glassRight,
        double glassBottom,
        double glassTop,
        bool handleOnLeft,
        IBrush fill,
        IPen pen)
    {
        var profileLeft = handleOnLeft ? outer.Left : glassRight;
        var profileRight = handleOnLeft ? glassLeft : outer.Right;
        if (profileRight - profileLeft <= CadMath.Epsilon)
        {
            return;
        }

        var axisX = (profileLeft + profileRight) / 2.0;
        var centerY = (glassBottom + glassTop) / 2.0;
        var baseHalfW = HandleBaseWidthMm / 2.0;
        var baseHalfH = Math.Min(HandleBaseHeightMm / 2.0, (glassTop - glassBottom) * 0.45);

        DrawWorldHandleRect(
            context,
            editor,
            axisX - baseHalfW,
            centerY - baseHalfH,
            axisX + baseHalfW,
            centerY + baseHalfH,
            fill,
            pen);

        var leverHalf = HandleLeverThicknessMm / 2.0;
        if (handleOnLeft)
        {
            var leverLength = Math.Min(
                HandleLeverLengthMm,
                Math.Max(0, glassRight - axisX - HandleGlassInsetMm));
            DrawWorldHandleRect(
                context,
                editor,
                axisX,
                centerY - leverHalf,
                axisX + leverLength,
                centerY + leverHalf,
                fill,
                pen);
        }
        else
        {
            var leverLength = Math.Min(
                HandleLeverLengthMm,
                Math.Max(0, axisX - glassLeft - HandleGlassInsetMm));
            DrawWorldHandleRect(
                context,
                editor,
                axisX - leverLength,
                centerY - leverHalf,
                axisX,
                centerY + leverHalf,
                fill,
                pen);
        }
    }

    private static void DrawHorizontalSashHandle(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.OuterBounds outer,
        double glassLeft,
        double glassRight,
        double glassBottom,
        double glassTop,
        bool handleOnBottom,
        IBrush fill,
        IPen pen)
    {
        var profileBottom = handleOnBottom ? outer.Bottom : glassTop;
        var profileTop = handleOnBottom ? glassBottom : outer.Top;
        if (profileTop - profileBottom <= CadMath.Epsilon)
        {
            return;
        }

        var axisY = (profileBottom + profileTop) / 2.0;
        var centerX = (glassLeft + glassRight) / 2.0;
        var baseHalfDepth = HandleBaseWidthMm / 2.0;
        var baseHalfSpan = Math.Min(HandleBaseHeightMm / 2.0, (glassRight - glassLeft) * 0.45);

        DrawWorldHandleRect(
            context,
            editor,
            centerX - baseHalfSpan,
            axisY - baseHalfDepth,
            centerX + baseHalfSpan,
            axisY + baseHalfDepth,
            fill,
            pen);

        var leverHalf = HandleLeverThicknessMm / 2.0;
        if (handleOnBottom)
        {
            var leverLength = Math.Min(
                HandleLeverLengthMm,
                Math.Max(0, glassTop - axisY - HandleGlassInsetMm));
            DrawWorldHandleRect(
                context,
                editor,
                centerX - leverHalf,
                axisY,
                centerX + leverHalf,
                axisY + leverLength,
                fill,
                pen);
        }
        else
        {
            var leverLength = Math.Min(
                HandleLeverLengthMm,
                Math.Max(0, axisY - glassBottom - HandleGlassInsetMm));
            DrawWorldHandleRect(
                context,
                editor,
                centerX - leverHalf,
                axisY - leverLength,
                centerX + leverHalf,
                axisY,
                fill,
                pen);
        }
    }

    private static void DrawWorldHandleRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double bottom,
        double right,
        double top,
        IBrush fill,
        IPen pen)
    {
        if (right - left <= CadMath.Epsilon || top - bottom <= CadMath.Epsilon)
        {
            return;
        }

        var rect = WorldBoundsToScreenRect(editor, left, bottom, right - left, top - bottom);
        context.DrawRectangle(fill, pen, rect);
    }

    private static CadPoint GetApexWorldPoint(
        double glassLeft,
        double glassBottom,
        double glassRight,
        double glassTop,
        SashHingeSide hingeSide) => hingeSide switch
    {
        SashHingeSide.Left => new CadPoint(glassRight, (glassBottom + glassTop) / 2.0),
        SashHingeSide.Right => new CadPoint(glassLeft, (glassBottom + glassTop) / 2.0),
        SashHingeSide.Top => new CadPoint((glassLeft + glassRight) / 2.0, glassBottom),
        SashHingeSide.Bottom => new CadPoint((glassLeft + glassRight) / 2.0, glassTop),
        _ => new CadPoint(glassRight, (glassBottom + glassTop) / 2.0)
    };

    private static (CadPoint A, CadPoint B) GetHingeCornerWorldPoints(
        double glassLeft,
        double glassBottom,
        double glassRight,
        double glassTop,
        SashHingeSide hingeSide) => hingeSide switch
    {
        SashHingeSide.Left => (new CadPoint(glassLeft, glassTop), new CadPoint(glassLeft, glassBottom)),
        SashHingeSide.Right => (new CadPoint(glassRight, glassTop), new CadPoint(glassRight, glassBottom)),
        SashHingeSide.Top => (new CadPoint(glassLeft, glassTop), new CadPoint(glassRight, glassTop)),
        SashHingeSide.Bottom => (new CadPoint(glassLeft, glassBottom), new CadPoint(glassRight, glassBottom)),
        _ => (new CadPoint(glassLeft, glassTop), new CadPoint(glassLeft, glassBottom))
    };

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint point)
    {
        var p = editor.Viewport!.WorldToScreen(point);
        return new Point(p.X, p.Y);
    }
}
