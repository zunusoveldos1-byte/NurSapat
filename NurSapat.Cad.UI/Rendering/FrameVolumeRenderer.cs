using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Collections.Generic;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Отрисовка коробки рамы с объёмным профилем и угловыми стыками под 45°.
/// Координаты: X/Y — нижний левый угол, Y растёт вверх.
/// </summary>
public static class FrameVolumeRenderer
{
    public static readonly Color MiterColor = Color.Parse("#919191");
    public static readonly Color ProfileStrokeColor = Color.Parse("#919191");
    public static readonly Color SelectionHighlightColor = Color.Parse("#FF0404");
    public static readonly Color DummySelectionHighlightColor = Color.Parse("#0078D7");

    public const double ProfileStrokeThickness = 1.5;

    private static readonly IBrush InnerOpeningFill = Brushes.White;
    private static readonly IBrush ProfileFill = new SolidColorBrush(Color.FromArgb(48, 0xE5, 0xE5, 0xE5));
    private static readonly IBrush ProfileStrokeBrush = new SolidColorBrush(ProfileStrokeColor);
    private static readonly IBrush MiterBrush = new SolidColorBrush(MiterColor);
    public static readonly IBrush SelectionHighlightBrush = new SolidColorBrush(SelectionHighlightColor);
    public static readonly IBrush DummySelectionHighlightBrush = new SolidColorBrush(DummySelectionHighlightColor);
    public static readonly IPen ProfilePen = new Pen(ProfileStrokeBrush, ProfileStrokeThickness);
    private static readonly IPen MiterPen = new Pen(MiterBrush, 1.2);
    public static readonly IPen DashedProfilePen = new Pen(
        ProfileStrokeBrush,
        ProfileStrokeThickness,
        new DashStyle(new double[] { 4, 3 }, 0));
    public static readonly IPen TopProfileHighlightPen = new Pen(
        SelectionHighlightBrush,
        3.0);
    public static readonly IPen SelectionDimensionPen = new Pen(SelectionHighlightBrush, 1.2);

    public static IPen ResolveProfileOutlinePen(bool dashed) =>
        dashed ? DashedProfilePen : ProfilePen;

    public static void DrawFictitiousOutline(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness)
    {
        if (editor.Viewport is null || width <= 0 || height <= 0)
        {
            return;
        }

        var x0 = originX;
        var y0 = originY;
        var x1 = originX + width;
        var y1 = originY + height;
        var pen = DashedProfilePen;

        DrawWorldLine(context, editor, pen, new CadPoint(x0, y0), new CadPoint(x1, y0));
        DrawWorldLine(context, editor, pen, new CadPoint(x1, y0), new CadPoint(x1, y1));
        DrawWorldLine(context, editor, pen, new CadPoint(x1, y1), new CadPoint(x0, y1));
        DrawWorldLine(context, editor, pen, new CadPoint(x0, y1), new CadPoint(x0, y0));
    }

    public static void DrawFictitiousArchOutline(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double boxHeight,
        ProfileSegment topProfile)
    {
        if (editor.Viewport is null || width <= 0 || boxHeight <= 0)
        {
            return;
        }

        var pen = new Pen(ProfileStrokeBrush, 1.0);
        var bottomLeft = new CadPoint(originX, originY);
        var bottomRight = new CadPoint(originX + width, originY);
        var springingLeft = new CadPoint(originX, originY + boxHeight);
        var springingRight = new CadPoint(originX + width, originY + boxHeight);

        DrawWorldLine(context, editor, pen, bottomLeft, bottomRight);
        DrawWorldLine(context, editor, pen, bottomLeft, springingLeft);
        DrawWorldLine(context, editor, pen, bottomRight, springingRight);

        if (topProfile.SegmentType == SegmentType.Arc && topProfile.ArcHeight > CadMath.Epsilon)
        {
            var center = topProfile.GetArcCenterWorld();
            var outerRadius = ProfileSegment.ComputeRadiusFromSagitta(topProfile.ChordWidth, topProfile.ArcHeight);
            var (startAngle, endAngle) = topProfile.GetOuterArcAnglesRad();
            var previous = WorldToScreen(editor, springingLeft);
            foreach (var worldPoint in CircularArc.TessellateSpringingArc(center, outerRadius, startAngle, endAngle))
            {
                var current = WorldToScreen(editor, worldPoint);
                context.DrawLine(pen, previous, current);
                previous = current;
            }
        }
        else
        {
            DrawWorldLine(context, editor, pen, springingLeft, springingRight);
        }
    }

    public static void Draw(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        bool dashed = false,
        bool opaqueProfile = false,
        ProfileSegment? topProfile = null)
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

        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThickness) / 2.0;
        if (halfThickness <= CadMath.Epsilon)
        {
            DrawFallbackOutline(context, editor, originX, originY, width, height, dashed);
            return;
        }

        var x0 = originX;
        var y0 = originY;
        var x1 = originX + width;
        var y1 = originY + height;
        var xi0 = x0 + halfThickness;
        var xi1 = x1 - halfThickness;
        var yi0 = y0 + halfThickness;
        var yi1 = y1 - halfThickness;

        var hasArcTop = topProfile?.SegmentType == SegmentType.Arc &&
                        topProfile.Radius > CadMath.Epsilon &&
                        topProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon;
        var outlinePen = dashed ? DashedProfilePen : ProfilePen;

        if (hasArcTop)
        {
            var contours = FrameProfileGeometry.BuildArchedFrameContours(
                originX,
                originY,
                width,
                height,
                topProfile!,
                profileThickness);

            FillContourLoop(context, editor, contours.OpeningLoop, InnerOpeningFill, null);
            DrawProfileBand(context, editor, contours.OuterLoop, contours.InnerLoop, profileFill, null);
            StrokeContourLoop(context, editor, contours.OuterLoop, outlinePen);
            StrokeContourLoop(context, editor, contours.InnerLoop, outlinePen);
            DrawCornerMiter(context, editor, contours.OuterSpringingLeft, contours.InnerSpringingLeft);
            DrawCornerMiter(context, editor, contours.OuterSpringingRight, contours.InnerSpringingRight);
        }
        else
        {
            FillInnerOpening(context, editor, xi0, yi0, xi1, yi1);

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
        }

        DrawCornerMiter(context, editor, new CadPoint(x0, y0), new CadPoint(xi0, yi0));
        DrawCornerMiter(context, editor, new CadPoint(x1, y0), new CadPoint(xi1, yi0));
        if (!hasArcTop)
        {
            DrawCornerMiter(context, editor, new CadPoint(x0, y1), new CadPoint(xi0, yi1));
            DrawCornerMiter(context, editor, new CadPoint(x1, y1), new CadPoint(xi1, yi1));
        }
    }

    /// <summary>
    /// Переходной профиль: только кольцо адаптера по периметру проёма без
    /// отдельной внутренней коробки и без размерных линий.
    /// </summary>
    public static void DrawTransitionProfileBand(
        DrawingContext context,
        CadEditorViewModel editor,
        double originX,
        double originY,
        double width,
        double height,
        double profileThicknessMm,
        IReadOnlyDictionary<ImpostEdgeAlignment, double>? extremeImpostAxes = null)
    {
        if (editor.Viewport is null || width <= 0 || height <= 0)
        {
            return;
        }

        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThicknessMm) / 2.0;
        if (halfThickness <= CadMath.Epsilon)
        {
            DrawFallbackOutline(context, editor, originX, originY, width, height, dashed: false);
            return;
        }

        var x0 = originX;
        var y0 = originY;
        var x1 = originX + width;
        var y1 = originY + height;

        double xi0Bl, xi1Br, xi0Tl, xi1Tr, yi0Bl, yi0Br, yi1Tl, yi1Tr;
        if (editor.FrameRoot is { } frameRoot)
        {
            var centerX = originX + width / 2.0;
            var centerY = originY + height / 2.0;
            var leaf = frameRoot.FindLeafAt(centerX, centerY);
            if (leaf is not null)
            {
                SashGeometry.ApplyExtremeImpostJunctionToTransitionBandOuter(
                    leaf,
                    frameRoot,
                    editor.FrameThickness,
                    ref x0,
                    ref y0,
                    ref x1,
                    ref y1);

                if (x1 <= x0 + CadMath.Epsilon || y1 <= y0 + CadMath.Epsilon)
                {
                    return;
                }

                SashGeometry.ResolveTransitionBandInnerCorners(
                    leaf,
                    frameRoot,
                    editor.FrameThickness,
                    x0,
                    y0,
                    x1,
                    y1,
                    halfThickness,
                    out var innerLeftBottom,
                    out var innerRightBottom,
                    out var innerLeftTop,
                    out var innerRightTop,
                    out var innerBottomLeft,
                    out var innerTopLeft,
                    out var innerBottomRight,
                    out var innerTopRight);
                xi0Bl = innerLeftBottom;
                xi1Br = innerRightBottom;
                xi0Tl = innerLeftTop;
                xi1Tr = innerRightTop;
                yi0Bl = innerBottomLeft;
                yi0Br = innerBottomRight;
                yi1Tl = innerTopLeft;
                yi1Tr = innerTopRight;
            }
            else
            {
                xi0Bl = xi0Tl = x0 + halfThickness;
                xi1Br = xi1Tr = x1 - halfThickness;
                yi0Bl = yi0Br = y0 + halfThickness;
                yi1Tl = yi1Tr = y1 - halfThickness;
            }
        }
        else
        {
            xi0Bl = xi0Tl = x0 + halfThickness;
            xi1Br = xi1Tr = x1 - halfThickness;
            yi0Bl = yi0Br = y0 + halfThickness;
            yi1Tl = yi1Tr = y1 - halfThickness;

            if (extremeImpostAxes is not null)
            {
                foreach (var (edge, axis) in extremeImpostAxes)
                {
                    switch (edge)
                    {
                        case ImpostEdgeAlignment.Left:
                            x0 = Math.Max(x0, axis);
                            xi0Bl = xi0Tl = Math.Max(xi0Bl, axis);
                            break;
                        case ImpostEdgeAlignment.Right:
                            x1 = Math.Min(x1, axis);
                            xi1Br = xi1Tr = Math.Min(xi1Br, axis);
                            break;
                        case ImpostEdgeAlignment.Bottom:
                            y0 = Math.Max(y0, axis);
                            yi0Bl = yi0Br = Math.Max(yi0Bl, axis);
                            break;
                        case ImpostEdgeAlignment.Top:
                            y1 = Math.Min(y1, axis);
                            yi1Tl = yi1Tr = Math.Min(yi1Tl, axis);
                            break;
                    }
                }
            }
        }

        DrawRail(context, editor, ProfilePen, ProfileFill,
            new CadPoint(x0, y0), new CadPoint(x1, y0),
            new CadPoint(xi1Br, yi0Br), new CadPoint(xi0Bl, yi0Bl));

        DrawRail(context, editor, ProfilePen, ProfileFill,
            new CadPoint(x0, y1), new CadPoint(x1, y1),
            new CadPoint(xi1Tr, yi1Tr), new CadPoint(xi0Tl, yi1Tl));

        DrawRail(context, editor, ProfilePen, ProfileFill,
            new CadPoint(x0, y0), new CadPoint(x0, y1),
            new CadPoint(xi0Tl, yi1Tl), new CadPoint(xi0Bl, yi0Bl));

        DrawRail(context, editor, ProfilePen, ProfileFill,
            new CadPoint(x1, y0), new CadPoint(x1, y1),
            new CadPoint(xi1Tr, yi1Tr), new CadPoint(xi1Br, yi0Br));

        DrawCornerMiter(context, editor, new CadPoint(x0, y0), new CadPoint(xi0Bl, yi0Bl));
        DrawCornerMiter(context, editor, new CadPoint(x1, y0), new CadPoint(xi1Br, yi0Br));
        DrawCornerMiter(context, editor, new CadPoint(x0, y1), new CadPoint(xi0Tl, yi1Tl));
        DrawCornerMiter(context, editor, new CadPoint(x1, y1), new CadPoint(xi1Tr, yi1Tr));
    }

    public static void DrawArchedProfileContours(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop outerLoop,
        ProfileContourLoop innerLoop,
        bool dashed,
        bool opaqueProfile,
        CadPoint outerBottomLeft,
        CadPoint innerBottomLeft,
        CadPoint outerBottomRight,
        CadPoint innerBottomRight,
        CadPoint outerSpringingLeft,
        CadPoint innerSpringingLeft,
        CadPoint outerSpringingRight,
        CadPoint innerSpringingRight)
    {
        if (editor.Viewport == null)
        {
            return;
        }

        var profileFill = opaqueProfile ? InnerOpeningFill : ProfileFill;
        var outlinePen = dashed ? DashedProfilePen : ProfilePen;

        FillContourLoop(context, editor, innerLoop, InnerOpeningFill, null);
        DrawProfileBand(context, editor, outerLoop, innerLoop, profileFill, null);
        StrokeContourLoop(context, editor, outerLoop, outlinePen);
        StrokeContourLoop(context, editor, innerLoop, outlinePen);
        DrawProfileMiter(context, editor, outerBottomLeft, innerBottomLeft);
        DrawProfileMiter(context, editor, outerBottomRight, innerBottomRight);
        DrawProfileMiter(context, editor, outerSpringingLeft, innerSpringingLeft);
        DrawProfileMiter(context, editor, outerSpringingRight, innerSpringingRight);
    }

    public static void DrawTopProfileHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileSegment segment)
    {
        if (editor.Viewport == null || segment.ChordWidth <= CadMath.Epsilon)
        {
            return;
        }

        if (segment.SegmentType == SegmentType.Arc && segment.Radius > CadMath.Epsilon)
        {
            var center = segment.GetArcCenterWorld();
            var outerRadius = ProfileSegment.ComputeRadiusFromSagitta(segment.ChordWidth, segment.ArcHeight);
            var (startAngle, endAngle) = segment.GetOuterArcAnglesRad();
            var edge = ProfileEdge.Arc(center, outerRadius, startAngle, endAngle);
            StrokeProfileEdge(context, editor, edge, TopProfileHighlightPen);
            return;
        }

        context.DrawLine(
            TopProfileHighlightPen,
            WorldToScreen(editor, segment.Start),
            WorldToScreen(editor, segment.End));
    }

    public static void DrawArcVertexHandles(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileSegment segment)
    {
        if (editor.Viewport == null || segment.SegmentType != SegmentType.Arc || segment.Radius <= CadMath.Epsilon)
        {
            return;
        }

        var springingHandle = segment.GetArcSpringingHandleWorld();
        var handleBrush = SelectionHighlightBrush;

        DrawArcHandleEllipse(context, editor, springingHandle, handleBrush);

        if (editor.CurrentToolMode == WindowToolMode.ArcVertex)
        {
            DrawArcHandleEllipse(context, editor, segment.GetArcApexWorld(), handleBrush);
        }
    }

    private static void DrawArcHandleEllipse(
        DrawingContext context,
        CadEditorViewModel editor,
        CadPoint worldPoint,
        IBrush fill)
    {
        var screen = WorldToScreen(editor, worldPoint);
        const double radiusPx = 5.5;
        context.DrawEllipse(fill, TopProfileHighlightPen, screen, radiusPx, radiusPx);
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
        FrameProfileGeometry.ResolveInnerInset(width, height, profileThickness);

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

    private static void DrawProfileBand(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop outerLoop,
        ProfileContourLoop innerLoop,
        IBrush fill,
        IPen? pen)
    {
        var outerPoints = outerLoop.Tessellate();
        var innerPoints = innerLoop.Tessellate();
        if (outerPoints.Count < 3 || innerPoints.Count < 3)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(WorldToScreen(editor, outerPoints[0]), isFilled: true);
            for (var i = 1; i < outerPoints.Count; i++)
            {
                ctx.LineTo(WorldToScreen(editor, outerPoints[i]));
            }

            for (var i = innerPoints.Count - 1; i >= 0; i--)
            {
                ctx.LineTo(WorldToScreen(editor, innerPoints[i]));
            }

            ctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static void FillContourLoop(
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
            AppendLoop(ctx, editor, points, isFilled: true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static void StrokeContourLoop(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IPen pen)
    {
        foreach (var edge in loop.Edges)
        {
            StrokeProfileEdge(context, editor, edge, pen);
        }
    }

    private static void StrokeProfileEdge(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileEdge edge,
        IPen pen)
    {
        if (edge.Kind == ProfileEdgeKind.Line)
        {
            context.DrawLine(pen, WorldToScreen(editor, edge.Start), WorldToScreen(editor, edge.End));
            return;
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

    private static void AppendLoop(
        StreamGeometryContext ctx,
        CadEditorViewModel editor,
        IReadOnlyList<CadPoint> worldPoints,
        bool isFilled)
    {
        if (worldPoints.Count == 0)
        {
            return;
        }

        ctx.BeginFigure(WorldToScreen(editor, worldPoints[0]), isFilled);
        for (var i = 1; i < worldPoints.Count; i++)
        {
            ctx.LineTo(WorldToScreen(editor, worldPoints[i]));
        }

        ctx.EndFigure(isFilled);
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

    public static (bool IsLargeArc, SweepDirection Sweep) ResolveUpwardScreenArc(
        Point arcFrom,
        Point arcTo,
        ProfileSegment segment)
    {
        _ = arcFrom;
        _ = arcTo;
        var center = segment.GetArcCenterWorld();
        var (isLargeArc, ccwInWorld) = ProfileSegment.ResolveUpperBranchArcSweep(
            center,
            segment.Radius,
            segment.End,
            segment.Start);
        var sweep = ccwInWorld ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
        return (isLargeArc, sweep);
    }

    private static void DrawCornerMiter(DrawingContext context, CadEditorViewModel editor, CadPoint a, CadPoint b)
    {
        DrawProfileMiter(context, editor, a, b);
    }

    public static void DrawProfileMiter(DrawingContext context, CadEditorViewModel editor, CadPoint a, CadPoint b)
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
        context.DrawLine(pen, WorldToScreen(editor, start), WorldToScreen(editor, end));
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
