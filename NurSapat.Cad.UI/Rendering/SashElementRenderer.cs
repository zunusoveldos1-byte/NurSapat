using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Rendering;

public static class SashElementRenderer
{
    private static readonly IBrush OpeningLineBrush = new SolidColorBrush(Color.Parse("#696969"));
    private static readonly IPen OpeningIndicatorPen = new Pen(
        OpeningLineBrush,
        1.0,
        new DashStyle(new double[] { 4, 4 }, 0));
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
        foreach (var element in SnapshotElements(elements))
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                DrawProfile(context, editor, element);
            }
        }
    }

    public static void DrawOverlays(DrawingContext context, CadEditorViewModel editor, IEnumerable<GeometryElement> elements)
    {
        DrawOpeningIndicators(context, editor, elements);
        DrawHardwareOverlays(context, editor, elements);
    }

    public static void DrawOpeningIndicators(DrawingContext context, CadEditorViewModel editor, IEnumerable<GeometryElement> elements)
    {
        foreach (var element in SnapshotElements(elements))
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                DrawOpeningIndicator(context, editor, element);
            }
        }
    }

    public static void DrawHardwareOverlays(DrawingContext context, CadEditorViewModel editor, IEnumerable<GeometryElement> elements)
    {
        foreach (var element in SnapshotElements(elements))
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                DrawHardwareOverlay(context, editor, element);
            }
        }
    }

    public static void Draw(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        DrawProfile(context, editor, element);
        DrawOpeningIndicator(context, editor, element);
        DrawHardwareOverlay(context, editor, element);
    }

    public static void DrawProfile(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (editor?.Viewport == null || element.Kind != GeometryElementKind.Sash)
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

        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return;
        }

        var profileThickness = ResolveSashDrawProfileThickness();
        DrawSashProfileCore(context, editor, inner, outer, profileThickness, dashed: false, opaqueProfile: true);
    }

    private static void DrawSashProfileCore(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.OuterBounds outer,
        double profileThickness,
        bool dashed,
        bool opaqueProfile)
    {
        if (SashGeometry.TryBuildArchedSashContours(
                editor,
                inner,
                outer,
                profileThickness,
                out var outerLoop,
                out var innerLoop,
                out var outerBottomLeft,
                out var innerBottomLeft,
                out var outerBottomRight,
                out var innerBottomRight,
                out var outerSpringingLeft,
                out var innerSpringingLeft,
                out var outerSpringingRight,
                out var innerSpringingRight))
        {
            FrameVolumeRenderer.DrawArchedProfileContours(
                context,
                editor,
                outerLoop,
                innerLoop,
                dashed,
                opaqueProfile,
                outerBottomLeft,
                innerBottomLeft,
                outerBottomRight,
                innerBottomRight,
                outerSpringingLeft,
                innerSpringingLeft,
                outerSpringingRight,
                innerSpringingRight);
            return;
        }

        FrameVolumeRenderer.Draw(
            context,
            editor,
            outer.Left,
            outer.Bottom,
            outer.Width,
            outer.Height,
            profileThickness,
            dashed,
            opaqueProfile);
    }

    private static void DrawOpeningIndicator(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (editor?.Viewport == null || element.Kind != GeometryElementKind.Sash)
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

        if (!element.HasHardware)
        {
            return;
        }

        var opening = element.OpeningType ?? SashOpeningType.TiltTurn;
        if (!ShouldDrawOpeningDirectionLines(opening))
        {
            return;
        }

        var hinge = element.HingeSide ?? SashHingeSide.Left;
        DrawOpeningDirectionLines(context, editor, inner, opening, hinge, OpeningIndicatorPen);
    }

    private static void DrawHardwareOverlay(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        if (editor?.Viewport == null || element.Kind != GeometryElementKind.Sash)
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
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return;
        }

        SashGeometry.TryResolveGlassOpeningBounds(
            editor,
            inner,
            outer,
            ResolveSashDrawProfileThickness(),
            out var glass);
        if (glass.Width <= CadMath.Epsilon || glass.Height <= CadMath.Epsilon)
        {
            return;
        }

        if (element.HasHandle)
        {
            DrawHandleOnGlass(
                context,
                editor,
                inner,
                glass,
                outer,
                hinge,
                preview: false);
        }

        if (element.HasHardware)
        {
            DrawSashHinges(context, editor, inner, outer, hinge, opening, preview: false);
        }
    }

    public static void DrawPreview(DrawingContext context, CadEditorViewModel editor, SashDragPreview preview)
    {
        if (editor?.Viewport == null || preview.Width <= 0 || preview.Height <= 0)
        {
            return;
        }

        var inner = new SashGeometry.InnerOpeningBounds
        {
            Left = preview.OriginX,
            Bottom = preview.OriginY,
            Right = preview.OriginX + preview.Width,
            Top = preview.OriginY + preview.Height
        };

        DrawSashBody(
            context,
            editor,
            inner,
            preview.HingeSide,
            SashOpeningType.TiltTurn,
            hasHardware: false,
            hasHandle: false,
            preview: true);

        DrawOpeningDirectionLines(
            context,
            editor,
            inner,
            SashOpeningType.Turn,
            preview.HingeSide,
            PreviewPen);
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

        var inner = SashGeometry.TryResolveInnerBounds(editor, sash, out var liveInner)
            ? liveInner
            : SashGeometry.InnerOpeningBoundsFromElement(sash);
        SashGeometry.TryResolveGlassOpeningBounds(
            editor,
            inner,
            outer,
            ResolveSashDrawProfileThickness(),
            out var glass);
        if (glass.Width <= CadMath.Epsilon || glass.Height <= CadMath.Epsilon)
        {
            return;
        }

        DrawHandleOnGlass(
            context,
            editor,
            inner,
            glass,
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

        DrawSashProfileCore(
            context,
            editor,
            inner,
            outer,
            ResolveSashDrawProfileThickness(),
            dashed: preview,
            opaqueProfile: !preview);

        SashGeometry.TryResolveGlassOpeningBounds(
            editor,
            inner,
            outer,
            ResolveSashDrawProfileThickness(),
            out var glass);
        if (glass.Width <= CadMath.Epsilon || glass.Height <= CadMath.Epsilon)
        {
            return;
        }

        if (glass.Width <= CadMath.Epsilon || glass.Height <= CadMath.Epsilon)
        {
            return;
        }

        if (hasHandle)
        {
            DrawHandleOnGlass(
                context,
                editor,
                inner,
                glass,
                outer,
                hingeSide,
                preview);
        }

        if (hasHardware && !preview)
        {
            DrawSashHinges(context, editor, inner, outer, hingeSide, openingType, preview);
        }
    }

    private static double ResolveSashDrawProfileThickness() => SashGeometry.SashDrawProfileThicknessMm;

    private static (double Left, double Bottom, double Right, double Top, SashGeometry.OuterBounds Outer)
        ResolveHandleWorldBounds(CadEditorViewModel editor, GeometryElement element)
    {
        if (!SashGeometry.TryResolveGlassForSash(editor, element, out var glass, out var outer, out _))
        {
            return (0, 0, 0, 0, default);
        }

        return (glass.Left, glass.Bottom, glass.Right, glass.Top, outer);
    }

    /// <summary>
    /// Пунктирные линии направления открывания по световому проёму створки.
    /// Один треугольник на створку: поворотный или откидной — в зависимости от OpeningType и HingeSide.
    /// </summary>
    private static void DrawOpeningDirectionLines(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashOpeningType opening,
        SashHingeSide hingeSide,
        IPen pen)
    {
        switch (opening)
        {
            case SashOpeningType.Turn:
            case SashOpeningType.TiltTurn:
                if (IsSideHungHinge(hingeSide))
                {
                    DrawSideTurnOpeningLines(context, editor, pen, hingeSide, inner);
                }
                else
                {
                    DrawTiltOpeningLines(context, editor, pen, hingeSide, inner);
                }

                break;
        }
    }

    private static bool IsSideHungHinge(SashHingeSide hingeSide) =>
        hingeSide is SashHingeSide.Left or SashHingeSide.Right;

    /// <summary>Поворотное: линии от углов стороны петель к середине стороны ручки.</summary>
    private static void DrawSideTurnOpeningLines(
        DrawingContext context,
        CadEditorViewModel editor,
        IPen pen,
        SashHingeSide hingeSide,
        SashGeometry.InnerOpeningBounds inner)
    {
        var topLeft = SashGeometry.ResolveInnerCorner(editor, inner, inner.Left, inner.Top);
        var bottomLeft = new CadPoint(inner.Left, inner.Bottom);
        var topRight = SashGeometry.ResolveInnerCorner(editor, inner, inner.Right, inner.Top);
        var bottomRight = new CadPoint(inner.Right, inner.Bottom);
        var midLeft = new CadPoint(inner.Left, (inner.Bottom + inner.Top) / 2.0);
        var midRight = new CadPoint(inner.Right, (inner.Bottom + inner.Top) / 2.0);

        switch (hingeSide)
        {
            case SashHingeSide.Left:
                DrawOpeningLine(context, editor, pen, topLeft, midRight);
                DrawOpeningLine(context, editor, pen, bottomLeft, midRight);
                break;
            case SashHingeSide.Right:
                DrawOpeningLine(context, editor, pen, topRight, midLeft);
                DrawOpeningLine(context, editor, pen, bottomRight, midLeft);
                break;
        }
    }

    /// <summary>Откидное / фрамужное: линии к нижней или верхней стороне (без бокового треугольника).</summary>
    private static void DrawTiltOpeningLines(
        DrawingContext context,
        CadEditorViewModel editor,
        IPen pen,
        SashHingeSide hingeSide,
        SashGeometry.InnerOpeningBounds inner)
    {
        var topLeft = SashGeometry.ResolveInnerCorner(editor, inner, inner.Left, inner.Top);
        var bottomLeft = new CadPoint(inner.Left, inner.Bottom);
        var topRight = SashGeometry.ResolveInnerCorner(editor, inner, inner.Right, inner.Top);
        var bottomRight = new CadPoint(inner.Right, inner.Bottom);
        var midTop = new CadPoint(
            (inner.Left + inner.Right) / 2.0,
            (SashGeometry.TryResolveInnerArcTopY(editor, inner, (inner.Left + inner.Right) / 2.0, out var arcMidTop)
                ? arcMidTop
                : inner.Top));
        var midBottom = new CadPoint((inner.Left + inner.Right) / 2.0, inner.Bottom);

        switch (hingeSide)
        {
            case SashHingeSide.Bottom:
                DrawOpeningLine(context, editor, pen, topLeft, midBottom);
                DrawOpeningLine(context, editor, pen, topRight, midBottom);
                break;
            case SashHingeSide.Top:
                DrawOpeningLine(context, editor, pen, bottomLeft, midTop);
                DrawOpeningLine(context, editor, pen, bottomRight, midTop);
                break;
        }
    }

    private static void DrawOpeningLine(
        DrawingContext context,
        CadEditorViewModel editor,
        IPen pen,
        CadPoint from,
        CadPoint to) =>
        context.DrawLine(pen, WorldToScreen(editor, from), WorldToScreen(editor, to));

    private static bool ShouldDrawOpeningDirectionLines(SashOpeningType opening) =>
        opening is SashOpeningType.Turn or SashOpeningType.TiltTurn;

    private static void DrawSashHinges(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
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
                DrawVerticalSashHinges(context, editor, inner, outer, effectiveSide, pen);
                break;
            case SashHingeSide.Top:
                DrawHorizontalSashHinges(context, editor, inner, outer, mountOnTop: true, pen);
                break;
            case SashHingeSide.Bottom:
                DrawHorizontalSashHinges(context, editor, inner, outer, mountOnTop: false, pen);
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
        SashGeometry.InnerOpeningBounds inner,
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

        var straightTop = SashGeometry.ResolveStraightVerticalZoneTopY(editor, inner, outer);
        var topHingeBottom = Math.Min(outer.Top - offset, straightTop);
        var topHingeTop = topHingeBottom - hingeHeight;
        if (topHingeTop < outer.Bottom + offset)
        {
            return;
        }

        var bottomHingeBottom = outer.Bottom + offset;
        var bottomHingeTop = bottomHingeBottom + hingeHeight;
        if (bottomHingeTop >= topHingeTop - CadMath.Epsilon)
        {
            return;
        }

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
        SashGeometry.InnerOpeningBounds inner,
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
            var mountY = SashGeometry.ShouldDrawArchedSash(editor, inner)
                ? SashGeometry.ResolveStraightVerticalZoneTopY(editor, inner, outer)
                : outer.Top;
            var innerY = mountY - HingeWidthMm;
            var outerY = mountY + hingeIntoFrame;
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
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.GlassOpeningBounds glass,
        SashGeometry.OuterBounds outer,
        SashHingeSide hingeSide,
        bool preview)
    {
        if (glass.Width <= CadMath.Epsilon || glass.Height <= CadMath.Epsilon)
        {
            return;
        }

        var fill = preview ? HandlePreviewFillBrush : HandleBackgroundBrush;
        var pen = preview ? PreviewPen : HandleOutlinePen;

        switch (hingeSide)
        {
            case SashHingeSide.Right:
                DrawVerticalSashHandle(context, editor, inner, glass, outer, handleOnLeft: true, fill, pen);
                break;
            case SashHingeSide.Left:
                DrawVerticalSashHandle(context, editor, inner, glass, outer, handleOnLeft: false, fill, pen);
                break;
            case SashHingeSide.Top:
                DrawHorizontalSashHandle(context, editor, outer, glass, handleOnBottom: true, fill, pen);
                break;
            case SashHingeSide.Bottom:
                DrawHorizontalSashHandle(context, editor, outer, glass, handleOnBottom: false, fill, pen);
                break;
        }
    }

    private static void DrawVerticalSashHandle(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.GlassOpeningBounds glass,
        SashGeometry.OuterBounds outer,
        bool handleOnLeft,
        IBrush fill,
        IPen pen)
    {
        var profileLeft = handleOnLeft ? outer.Left : glass.Right;
        var profileRight = handleOnLeft ? glass.Left : outer.Right;
        if (profileRight - profileLeft <= CadMath.Epsilon)
        {
            return;
        }

        var axisX = (profileLeft + profileRight) / 2.0;
        var baseHalfH = Math.Min(HandleBaseHeightMm / 2.0, glass.Height * 0.45);
        var centerY = SashGeometry.ClampHandleCenterY(
            editor,
            inner,
            outer,
            glass,
            axisX,
            (glass.Bottom + glass.Top) / 2.0,
            baseHalfH);
        var baseHalfW = HandleBaseWidthMm / 2.0;

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
                Math.Max(0, glass.Right - axisX - HandleGlassInsetMm));
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
                Math.Max(0, axisX - glass.Left - HandleGlassInsetMm));
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
        SashGeometry.GlassOpeningBounds glass,
        bool handleOnBottom,
        IBrush fill,
        IPen pen)
    {
        var profileBottom = handleOnBottom ? outer.Bottom : glass.Top;
        var profileTop = handleOnBottom ? glass.Bottom : outer.Top;
        if (profileTop - profileBottom <= CadMath.Epsilon)
        {
            return;
        }

        var axisY = (profileBottom + profileTop) / 2.0;
        var centerX = (glass.Left + glass.Right) / 2.0;
        var baseHalfDepth = HandleBaseWidthMm / 2.0;
        var baseHalfSpan = Math.Min(HandleBaseHeightMm / 2.0, glass.Width * 0.45);

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
                Math.Max(0, glass.ResolveTopAtX(centerX) - axisY - HandleGlassInsetMm));
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
                Math.Max(0, axisY - glass.Bottom - HandleGlassInsetMm));
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

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint point)
    {
        var p = editor.Viewport!.WorldToScreen(point);
        return new Point(p.X, p.Y);
    }

    private static List<GeometryElement> SnapshotElements(IEnumerable<GeometryElement> elements) =>
        elements as List<GeometryElement> ?? elements.ToList();
}
