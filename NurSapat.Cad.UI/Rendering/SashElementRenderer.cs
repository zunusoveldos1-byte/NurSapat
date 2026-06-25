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
    private static readonly IPen OpeningPen = new Pen(OpeningLineBrush, 1.0);
    private static readonly IPen PreviewPen = new Pen(new SolidColorBrush(Color.Parse("#0078D7")), 1, new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen HardwarePen = new Pen(new SolidColorBrush(FrameVolumeRenderer.ProfileStrokeColor), 1.2);
    private static readonly IBrush HandleBackgroundBrush = new SolidColorBrush(Colors.White);
    private static readonly IBrush HandlePreviewFillBrush = new SolidColorBrush(Color.FromArgb(48, 0, 120, 215));

    /// <summary>Длина рукояти в сторону стекла (мм).</summary>
    private const double HandleArmLengthMm = 120.0;
    /// <summary>Толщина рукояти (мм).</summary>
    private const double HandleArmThicknessMm = 24.0;
    /// <summary>Глубина розетки вдоль профиля створки (мм).</summary>
    private const double HandleRosetteDepthMm = 30.0;
    /// <summary>Высота розетки вдоль створки (мм).</summary>
    private const double HandleRosetteHeightMm = 100.0;
    private const double HandleGlassClearanceMm = 6.0;
    /// <summary>На сколько шейка отходит от розетки в сторону стекла (мм).</summary>
    private const double HandleNeckLengthMm = 12.0;
    /// <summary>Толщина шейки ручки (мм).</summary>
    private const double HandleNeckThicknessMm = 16.0;

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
        DrawSashBody(
            context,
            editor,
            inner,
            hinge,
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

        var glassRect = WorldGlassRect(editor, glassLeft, glassBottom, glassRight, glassTop);

        DrawOpeningTriangle(context, editor, glassLeft, glassBottom, glassRight, glassTop, hingeSide);

        if (hasHardware && glassRect.Width > 0 && glassRect.Height > 0)
        {
            DrawHingeBlocks(context, glassRect, hingeSide);
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

    private static Rect WorldGlassRect(
        CadEditorViewModel editor,
        double glassLeft,
        double glassBottom,
        double glassRight,
        double glassTop)
    {
        var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(glassLeft, glassTop));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(glassRight, glassBottom));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }

    private static void DrawOpeningTriangle(
        DrawingContext context,
        CadEditorViewModel editor,
        double glassLeft,
        double glassBottom,
        double glassRight,
        double glassTop,
        SashHingeSide hingeSide)
    {
        var apex = GetApexWorldPoint(glassLeft, glassBottom, glassRight, glassTop, hingeSide);
        var (hingeA, hingeB) = GetHingeCornerWorldPoints(glassLeft, glassBottom, glassRight, glassTop, hingeSide);

        var pApex = WorldToScreen(editor, apex);
        var pHingeA = WorldToScreen(editor, hingeA);
        var pHingeB = WorldToScreen(editor, hingeB);

        context.DrawLine(OpeningPen, pHingeA, pApex);
        context.DrawLine(OpeningPen, pHingeB, pApex);
    }

    private static void DrawHingeBlocks(DrawingContext context, Rect glassRect, SashHingeSide hingeSide)
    {
        const double block = 6;
        switch (hingeSide)
        {
            case SashHingeSide.Left:
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Left, glassRect.Top + 12, block, block));
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Left, glassRect.Bottom - 18, block, block));
                break;
            case SashHingeSide.Right:
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Right - block, glassRect.Top + 12, block, block));
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Right - block, glassRect.Bottom - 18, block, block));
                break;
            case SashHingeSide.Top:
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Left + 12, glassRect.Top, block, block));
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Right - 18, glassRect.Top, block, block));
                break;
            case SashHingeSide.Bottom:
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Left + 12, glassRect.Bottom - block, block, block));
                context.DrawRectangle(null, HardwarePen, new Rect(glassRect.Right - 18, glassRect.Bottom - block, block, block));
                break;
        }
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
        var centerX = (glassLeft + glassRight) / 2.0;
        var centerY = (glassBottom + glassTop) / 2.0;
        var glassWidth = glassRight - glassLeft;
        var glassHeight = glassTop - glassBottom;
        var fill = preview ? HandlePreviewFillBrush : HandleBackgroundBrush;
        var pen = ResolveHandlePen(preview);

        switch (hingeSide)
        {
            case SashHingeSide.Right:
                DrawVerticalHandleLayers(
                    context, editor, fill, pen,
                    handleOnLeft: true,
                    glassLeft, glassRight, centerY,
                    glassWidth, glassHeight,
                    Math.Max(0, glassLeft - outer.Left));
                break;
            case SashHingeSide.Left:
                DrawVerticalHandleLayers(
                    context, editor, fill, pen,
                    handleOnLeft: false,
                    glassLeft, glassRight, centerY,
                    glassWidth, glassHeight,
                    Math.Max(0, outer.Right - glassRight));
                break;
            case SashHingeSide.Top:
                DrawHorizontalHandleLayers(
                    context, editor, fill, pen,
                    handleOnBottom: true,
                    glassBottom, glassTop, centerX,
                    glassWidth, glassHeight, glassRight,
                    Math.Max(0, glassBottom - outer.Bottom));
                break;
            case SashHingeSide.Bottom:
                DrawHorizontalHandleLayers(
                    context, editor, fill, pen,
                    handleOnBottom: false,
                    glassBottom, glassTop, centerX,
                    glassWidth, glassHeight, glassRight,
                    Math.Max(0, outer.Top - glassTop));
                break;
        }
    }

    private static void DrawVerticalHandleLayers(
        DrawingContext context,
        CadEditorViewModel editor,
        IBrush fill,
        IPen pen,
        bool handleOnLeft,
        double glassLeft,
        double glassRight,
        double centerY,
        double glassWidth,
        double glassHeight,
        double profileDepth)
    {
        var dims = ResolveVerticalHandleDimensions(glassWidth, glassHeight, profileDepth);
        if (dims == null)
        {
            return;
        }

        var (rosetteDepth, rh2, neckLength, armLength, nh, ah) = dims.Value;

        CadPoint[] neckArmOutline;
        CadPoint[] rosetteOutline;
        CadPoint[] fullOutline;

        if (handleOnLeft)
        {
            var rosetteLeft = glassLeft - rosetteDepth;
            var neckEndX = glassLeft + neckLength;
            var armEndX = neckEndX + armLength;
            neckArmOutline = BuildNeckArmLeftVerticalOutline(glassLeft, centerY, neckEndX, armEndX, nh, ah);
            rosetteOutline = BuildRosetteVerticalOutline(rosetteLeft, glassLeft, centerY, rh2);
            fullOutline = BuildLeftSideVerticalOutline(
                rosetteLeft, glassLeft, centerY, rh2,
                glassLeft, neckEndX, armEndX, nh, ah);
        }
        else
        {
            var rosetteRight = glassRight + rosetteDepth;
            var neckStartX = glassRight - neckLength;
            var armStartX = neckStartX - armLength;
            neckArmOutline = BuildNeckArmRightVerticalOutline(glassRight, centerY, neckStartX, armStartX, nh, ah);
            rosetteOutline = BuildRosetteVerticalOutline(glassRight, rosetteRight, centerY, rh2);
            fullOutline = BuildRightSideVerticalOutline(
                glassRight, rosetteRight, centerY, rh2,
                glassRight, neckStartX, armStartX, nh, ah);
        }

        DrawFilledHandleGeometry(context, editor, fill, neckArmOutline);
        DrawFilledHandleGeometry(context, editor, fill, rosetteOutline);
        DrawStrokedHandleGeometry(context, editor, pen, fullOutline);
    }

    private static void DrawHorizontalHandleLayers(
        DrawingContext context,
        CadEditorViewModel editor,
        IBrush fill,
        IPen pen,
        bool handleOnBottom,
        double glassBottom,
        double glassTop,
        double centerX,
        double glassWidth,
        double glassHeight,
        double glassRight,
        double profileDepth)
    {
        var dims = ResolveHorizontalHandleDimensions(glassWidth, glassHeight, glassRight, centerX, profileDepth);
        if (dims == null)
        {
            return;
        }

        var (rosetteDepth, rs2, neckLength, armLength, nh, ah) = dims.Value;

        CadPoint[] neckArmOutline;
        CadPoint[] rosetteOutline;
        CadPoint[] fullOutline;

        if (handleOnBottom)
        {
            var neckEndY = glassBottom + neckLength;
            neckArmOutline = BuildNeckArmBottomOutline(centerX, glassBottom, neckEndY, armLength, nh, ah);
            rosetteOutline = BuildRosetteHorizontalOutline(centerX, rs2, glassBottom - rosetteDepth, glassBottom);
            fullOutline = BuildBottomSashOutline(centerX, glassBottom, rs2, rosetteDepth, neckEndY, armLength, nh, ah);
        }
        else
        {
            var neckStartY = glassTop - neckLength;
            neckArmOutline = BuildNeckArmTopOutline(centerX, glassTop, neckStartY, armLength, nh, ah);
            rosetteOutline = BuildRosetteHorizontalOutline(centerX, rs2, glassTop, glassTop + rosetteDepth);
            fullOutline = BuildTopSashOutline(centerX, glassTop, rs2, rosetteDepth, neckStartY, armLength, nh, ah);
        }

        DrawFilledHandleGeometry(context, editor, fill, neckArmOutline);
        DrawFilledHandleGeometry(context, editor, fill, rosetteOutline);
        DrawStrokedHandleGeometry(context, editor, pen, fullOutline);
    }

    private static (double RosetteDepth, double RosetteHalfSpan, double NeckLength, double ArmLength, double NeckHalf, double ArmHalf)?
        ResolveVerticalHandleDimensions(double glassWidth, double glassHeight, double profileDepth)
    {
        var rosetteDepth = Math.Min(HandleRosetteDepthMm, profileDepth * 0.92);
        var rosetteHeight = Math.Min(HandleRosetteHeightMm, glassHeight - HandleGlassClearanceMm);
        if (rosetteDepth <= 0 || rosetteHeight <= 0)
        {
            return null;
        }

        var availableWidth = Math.Max(0, glassWidth - HandleGlassClearanceMm);
        var neckLength = Math.Min(HandleNeckLengthMm, availableWidth);
        var armLength = Math.Min(HandleArmLengthMm, Math.Max(0, availableWidth - neckLength));
        if (neckLength <= 0 && armLength <= 0)
        {
            return null;
        }

        return (
            rosetteDepth,
            rosetteHeight / 2.0,
            neckLength,
            armLength,
            HandleNeckThicknessMm / 2.0,
            HandleArmThicknessMm / 2.0);
    }

    private static (double RosetteDepth, double RosetteHalfSpan, double NeckLength, double ArmLength, double NeckHalf, double ArmHalf)?
        ResolveHorizontalHandleDimensions(
            double glassWidth,
            double glassHeight,
            double glassRight,
            double centerX,
            double profileDepth)
    {
        var rosetteDepth = Math.Min(HandleRosetteDepthMm, profileDepth * 0.92);
        var rosetteSpan = Math.Min(HandleRosetteHeightMm, glassWidth - HandleGlassClearanceMm);
        if (rosetteDepth <= 0 || rosetteSpan <= 0)
        {
            return null;
        }

        var availableHeight = Math.Max(0, glassHeight - HandleGlassClearanceMm);
        var neckLength = Math.Min(HandleNeckLengthMm, availableHeight);
        var armLength = Math.Min(
            HandleArmLengthMm,
            Math.Max(0, glassRight - centerX - HandleGlassClearanceMm));
        if (neckLength <= 0 && armLength <= 0)
        {
            return null;
        }

        return (
            rosetteDepth,
            rosetteSpan / 2.0,
            neckLength,
            armLength,
            HandleNeckThicknessMm / 2.0,
            HandleArmThicknessMm / 2.0);
    }

    private static CadPoint[] BuildRosetteVerticalOutline(
        double innerEdge,
        double outerEdge,
        double centerY,
        double halfHeight) =>
    [
        new(outerEdge, centerY + halfHeight),
        new(outerEdge, centerY - halfHeight),
        new(innerEdge, centerY - halfHeight),
        new(innerEdge, centerY + halfHeight)
    ];

    private static CadPoint[] BuildRosetteHorizontalOutline(
        double centerX,
        double halfSpan,
        double outerEdge,
        double innerEdge) =>
    [
        new(centerX - halfSpan, innerEdge),
        new(centerX - halfSpan, outerEdge),
        new(centerX + halfSpan, outerEdge),
        new(centerX + halfSpan, innerEdge)
    ];

    private static CadPoint[] BuildNeckArmLeftVerticalOutline(
        double glassEdge,
        double centerY,
        double neckOuterX,
        double armOuterX,
        double nh,
        double ah)
    {
        var points = new List<CadPoint>(8)
        {
            new(glassEdge, centerY - nh),
            new(neckOuterX, centerY - nh)
        };

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuterX, centerY - ah));
        }

        points.Add(new(armOuterX, centerY - ah));
        points.Add(new(armOuterX, centerY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuterX, centerY + ah));
        }

        points.Add(new(neckOuterX, centerY + nh));
        points.Add(new(glassEdge, centerY + nh));
        return points.ToArray();
    }

    private static CadPoint[] BuildNeckArmRightVerticalOutline(
        double glassEdge,
        double centerY,
        double neckOuterX,
        double armOuterX,
        double nh,
        double ah)
    {
        var points = new List<CadPoint>(8)
        {
            new(glassEdge, centerY - nh),
            new(neckOuterX, centerY - nh)
        };

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuterX, centerY - ah));
        }

        points.Add(new(armOuterX, centerY - ah));
        points.Add(new(armOuterX, centerY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuterX, centerY + ah));
        }

        points.Add(new(neckOuterX, centerY + nh));
        points.Add(new(glassEdge, centerY + nh));
        return points.ToArray();
    }

    private static CadPoint[] BuildNeckArmBottomOutline(
        double centerX,
        double glassEdge,
        double neckOuterY,
        double armLength,
        double nh,
        double ah)
    {
        var armOuterX = centerX + nh + armLength;
        var points = new List<CadPoint>(8)
        {
            new(centerX + nh, glassEdge),
            new(centerX + nh, neckOuterY)
        };

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY));
        }

        points.Add(new(armOuterX, neckOuterY - ah));
        points.Add(new(armOuterX, neckOuterY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY + ah));
        }

        points.Add(new(centerX + nh, neckOuterY + ah));
        points.Add(new(centerX + nh, glassEdge));
        return points.ToArray();
    }

    private static CadPoint[] BuildNeckArmTopOutline(
        double centerX,
        double glassEdge,
        double neckOuterY,
        double armLength,
        double nh,
        double ah)
    {
        var armOuterX = centerX + nh + armLength;
        var points = new List<CadPoint>(8)
        {
            new(centerX + nh, glassEdge),
            new(centerX + nh, neckOuterY)
        };

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY));
        }

        points.Add(new(armOuterX, neckOuterY - ah));
        points.Add(new(armOuterX, neckOuterY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY + ah));
        }

        points.Add(new(centerX + nh, neckOuterY + ah));
        points.Add(new(centerX + nh, glassEdge));
        return points.ToArray();
    }

    private static void DrawFilledHandleGeometry(
        DrawingContext context,
        CadEditorViewModel editor,
        IBrush fill,
        IReadOnlyList<CadPoint> worldOutline)
    {
        var geometry = BuildHandleStreamGeometry(editor, worldOutline);
        if (geometry == null)
        {
            return;
        }

        context.DrawGeometry(fill, null, geometry);
    }

    private static void DrawStrokedHandleGeometry(
        DrawingContext context,
        CadEditorViewModel editor,
        IPen pen,
        IReadOnlyList<CadPoint> worldOutline)
    {
        var geometry = BuildHandleStreamGeometry(editor, worldOutline);
        if (geometry == null)
        {
            return;
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private static StreamGeometry? BuildHandleStreamGeometry(
        CadEditorViewModel editor,
        IReadOnlyList<CadPoint> worldOutline)
    {
        if (worldOutline.Count < 3)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(WorldToScreen(editor, worldOutline[0]), isFilled: true);
            for (var i = 1; i < worldOutline.Count; i++)
            {
                geometryContext.LineTo(WorldToScreen(editor, worldOutline[i]));
            }

            geometryContext.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static CadPoint[] BuildLeftSideVerticalOutline(
        double rosetteLeft,
        double glassEdge,
        double centerY,
        double rh2,
        double neckInner,
        double neckOuter,
        double armOuter,
        double nh,
        double ah)
    {
        var points = new List<CadPoint>(12)
        {
            new(rosetteLeft, centerY + rh2),
            new(rosetteLeft, centerY - rh2),
            new(glassEdge, centerY - rh2)
        };

        if (rh2 > nh + CadMath.Epsilon)
        {
            points.Add(new(glassEdge, centerY - nh));
        }

        points.Add(new(neckOuter, centerY - nh));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuter, centerY - ah));
        }

        points.Add(new(armOuter, centerY - ah));
        points.Add(new(armOuter, centerY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuter, centerY + ah));
            points.Add(new(neckOuter, centerY + nh));
        }
        else
        {
            points.Add(new(neckOuter, centerY + nh));
        }

        if (rh2 > nh + CadMath.Epsilon)
        {
            points.Add(new(glassEdge, centerY + nh));
            points.Add(new(glassEdge, centerY + rh2));
        }

        return points.ToArray();
    }

    private static CadPoint[] BuildRightSideVerticalOutline(
        double glassEdge,
        double rosetteRight,
        double centerY,
        double rh2,
        double neckInner,
        double neckOuter,
        double armOuter,
        double nh,
        double ah)
    {
        var points = new List<CadPoint>(12)
        {
            new(rosetteRight, centerY + rh2),
            new(rosetteRight, centerY - rh2),
            new(glassEdge, centerY - rh2)
        };

        if (rh2 > nh + CadMath.Epsilon)
        {
            points.Add(new(glassEdge, centerY - nh));
        }

        points.Add(new(neckOuter, centerY - nh));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuter, centerY - ah));
        }

        points.Add(new(armOuter, centerY - ah));
        points.Add(new(armOuter, centerY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(neckOuter, centerY + ah));
            points.Add(new(neckOuter, centerY + nh));
        }
        else
        {
            points.Add(new(neckOuter, centerY + nh));
        }

        if (rh2 > nh + CadMath.Epsilon)
        {
            points.Add(new(glassEdge, centerY + nh));
            points.Add(new(glassEdge, centerY + rh2));
        }

        return points.ToArray();
    }

    private static CadPoint[] BuildBottomSashOutline(
        double centerX,
        double glassEdge,
        double rs2,
        double rosetteDepth,
        double neckOuterY,
        double armLength,
        double nh,
        double ah)
    {
        var armOuterX = centerX + nh + armLength;
        var points = new List<CadPoint>(12)
        {
            new(centerX - rs2, glassEdge),
            new(centerX - rs2, glassEdge - rosetteDepth),
            new(centerX + rs2, glassEdge - rosetteDepth),
            new(centerX + rs2, glassEdge)
        };

        if (rs2 > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + nh, glassEdge));
        }

        points.Add(new(centerX + nh, neckOuterY));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY));
        }

        points.Add(new(armOuterX, neckOuterY - ah));
        points.Add(new(armOuterX, neckOuterY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY + ah));
            points.Add(new(centerX + nh, neckOuterY + ah));
        }
        else
        {
            points.Add(new(centerX + nh, neckOuterY + ah));
        }

        points.Add(new(centerX + nh, glassEdge));
        points.Add(new(centerX - rs2, glassEdge));

        return points.ToArray();
    }

    private static CadPoint[] BuildTopSashOutline(
        double centerX,
        double glassEdge,
        double rs2,
        double rosetteDepth,
        double neckOuterY,
        double armLength,
        double nh,
        double ah)
    {
        var armOuterX = centerX + nh + armLength;
        var points = new List<CadPoint>(12)
        {
            new(centerX - rs2, glassEdge),
            new(centerX - rs2, glassEdge + rosetteDepth),
            new(centerX + rs2, glassEdge + rosetteDepth),
            new(centerX + rs2, glassEdge)
        };

        if (rs2 > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + nh, glassEdge));
        }

        points.Add(new(centerX + nh, neckOuterY));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY));
        }

        points.Add(new(armOuterX, neckOuterY - ah));
        points.Add(new(armOuterX, neckOuterY + ah));

        if (ah > nh + CadMath.Epsilon)
        {
            points.Add(new(centerX + ah, neckOuterY + ah));
            points.Add(new(centerX + nh, neckOuterY + ah));
        }
        else
        {
            points.Add(new(centerX + nh, neckOuterY + ah));
        }

        points.Add(new(centerX + nh, glassEdge));
        points.Add(new(centerX - rs2, glassEdge));

        return points.ToArray();
    }

    private static IPen ResolveHandlePen(bool preview) => preview ? PreviewPen : HardwarePen;

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
