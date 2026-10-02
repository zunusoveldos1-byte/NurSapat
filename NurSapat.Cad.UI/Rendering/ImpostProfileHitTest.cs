using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

internal static class ImpostProfileHitTest
{
    private readonly record struct ImpostFootprint(
        double Left,
        double Right,
        double Bottom,
        double Top,
        ArcSpringingGeometry? Arc,
        IReadOnlyList<CadPoint>? HorizontalPolygon,
        bool IsTriangular = false,
        CadPoint TriBl = default,
        CadPoint TriBr = default,
        CadPoint TriTop = default);

    public static bool ContainsPoint(
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex,
        CadPoint point)
    {
        if (!TryGetFootprint(editor, node, gapIndex, out var footprint))
        {
            return false;
        }

        if (footprint.HorizontalPolygon is { } horizontalPolygon)
        {
            return HorizontalImpostGeometry.ContainsPoint(horizontalPolygon, point);
        }

        if (point.X < footprint.Left - CadMath.Epsilon ||
            point.X > footprint.Right + CadMath.Epsilon ||
            point.Y < footprint.Bottom - CadMath.Epsilon)
        {
            return false;
        }

        if (footprint.Arc is ArcSpringingGeometry arc)
        {
            var arcTopY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, point.X).Y;
            return point.Y <= arcTopY + CadMath.Epsilon;
        }

        if (footprint.IsTriangular)
        {
            return point.X >= footprint.Left - CadMath.Epsilon
                   && point.X <= footprint.Right + CadMath.Epsilon
                   && point.Y >= footprint.Bottom - CadMath.Epsilon
                   && point.Y <= FrameTriangleGeometry.InterpolateTopAtX(
                       footprint.TriBl,
                       footprint.TriBr,
                       footprint.TriTop,
                       point.X) + CadMath.Epsilon;
        }

        return point.Y <= footprint.Top + CadMath.Epsilon;
    }

    public static bool TryGetHorizontalFootprintPolygon(
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex,
        out IReadOnlyList<CadPoint> polygon)
    {
        polygon = Array.Empty<CadPoint>();
        if (!TryGetFootprint(editor, node, gapIndex, out var footprint) ||
            footprint.HorizontalPolygon is not { } horizontalPolygon)
        {
            return false;
        }

        polygon = horizontalPolygon;
        return true;
    }

    public static bool IntersectsRect(
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex,
        CadRect selection)
    {
        if (!TryGetFootprint(editor, node, gapIndex, out var footprint))
        {
            return false;
        }

        if (footprint.HorizontalPolygon is { } horizontalPolygon)
        {
            return HorizontalImpostGeometry.IntersectsRect(horizontalPolygon, selection);
        }

        if (footprint.Arc is ArcSpringingGeometry arc)
        {
            var polygon = BuildArcTrimmedPolygon(arc, footprint.Left, footprint.Right, footprint.Bottom);
            return ProfileBandHitTest.RectIntersectsPolygon(polygon, selection);
        }

        return selection.Intersects(ToCadRect(footprint));
    }

    public static bool TryGetFootprint(
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex,
        out double left,
        out double right,
        out double yBottom,
        out double rectangularTop,
        out ArcSpringingGeometry? arc)
    {
        left = right = yBottom = rectangularTop = 0;
        arc = null;

        if (!TryGetFootprint(editor, node, gapIndex, out var footprint))
        {
            return false;
        }

        left = footprint.Left;
        right = footprint.Right;
        yBottom = footprint.Bottom;
        rectangularTop = footprint.Top;
        arc = footprint.Arc;
        return true;
    }

    public static List<CadPoint> BuildArcTrimmedPolygon(
        ArcSpringingGeometry arc,
        double left,
        double right,
        double yBottom)
    {
        var points = new List<CadPoint>
        {
            new(left, yBottom),
            new(right, yBottom)
        };

        var span = right - left;
        var segments = Math.Clamp((int)Math.Ceiling(span / 15.0), 2, 16);
        for (var i = segments; i >= 0; i--)
        {
            var x = left + span * i / segments;
            var point = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, x);
            if (point.Y >= yBottom - CadMath.Epsilon)
            {
                points.Add(point);
            }
        }

        return points;
    }

    private static bool TryGetFootprint(
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex,
        out ImpostFootprint footprint)
    {
        footprint = default;

        if (node.IsUniformSplit)
        {
            return node.SplitOrientation switch
            {
                SplitOrientation.Vertical => TryGetUniformVerticalFootprint(editor, node, gapIndex, out footprint),
                SplitOrientation.Horizontal => TryGetUniformHorizontalFootprint(node, gapIndex, editor, out footprint),
                _ => false
            };
        }

        return node.SplitOrientation switch
        {
            SplitOrientation.Vertical => TryGetBinaryVerticalFootprint(editor, node, out footprint),
            SplitOrientation.Horizontal => TryGetBinaryHorizontalFootprint(editor, node, out footprint),
            _ => false
        };
    }

    private static bool TryGetUniformVerticalFootprint(
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex,
        out ImpostFootprint footprint)
    {
        footprint = default;
        if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var centerX))
        {
            return false;
        }

        if (!TryResolveVerticalImpostRenderBounds(centerX, ResolveImpostThickness(node), out var left, out var right))
        {
            return false;
        }

        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, node.GetRoot())
            && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, node.GetRoot(), node, centerX, out var roundBottom, out var roundTop))
        {
            footprint = new ImpostFootprint(left, right, roundBottom, roundTop, null, null);
            return true;
        }

        var yBottom = ResolveVerticalImpostBottom(editor, node);
        var rectangularTop = ResolveVerticalImpostSpan(editor, node, gapIndex).Top;
        ArcSpringingGeometry? arc = null;
        if (TryResolveVerticalImpostArcTrim(editor, node, centerX, left, right, rectangularTop, out var resolvedArc))
        {
            arc = resolvedArc;
        }

        if (editor.FrameRoot?.Shape == ConstructionShape.Triangle)
        {
            var innerTri = FrameTriangleGeometry.BuildInnerTriangle(editor.FrameRoot, editor.FrameThickness);
            footprint = new ImpostFootprint(
                left,
                right,
                yBottom,
                rectangularTop,
                null,
                null,
                IsTriangular: true,
                TriBl: innerTri.Bl,
                TriBr: innerTri.Br,
                TriTop: innerTri.Top);
            return true;
        }

        footprint = new ImpostFootprint(left, right, yBottom, rectangularTop, arc, null);
        return true;
    }

    private static bool TryGetUniformHorizontalFootprint(
        FrameNode node,
        int gapIndex,
        CadEditorViewModel editor,
        out ImpostFootprint footprint)
    {
        footprint = default;
        var cells = node.UniformCells;
        if (gapIndex < 0 || gapIndex >= cells.Count - 1)
        {
            return false;
        }

        if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var centerY)
            || !TryResolveHorizontalImpostRenderBounds(centerY, ResolveImpostThickness(node), out var bottom, out var top))
        {
            return false;
        }

        var span = HorizontalImpostGeometry.ResolveRectangularSpan(editor, node);
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, node.GetRoot())
            && node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var axisY)
            && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(editor, node.GetRoot(), node, axisY, out var circleLeft, out var circleRight))
        {
            span = (circleLeft, circleRight);
        }

        IReadOnlyList<CadPoint>? horizontalPolygon = null;
        if (HorizontalImpostGeometry.TryResolveArcAdaptedSpans(
                editor,
                node,
                bottom,
                top,
                out var leftBottom,
                out var rightBottom,
                out var leftTop,
                out var rightTop))
        {
            horizontalPolygon = HorizontalImpostGeometry.BuildFootprintPolygon(
                leftBottom,
                rightBottom,
                leftTop,
                rightTop,
                bottom,
                top);
            span = (leftBottom, rightBottom);
        }

        footprint = new ImpostFootprint(span.Left, span.Right, bottom, top, null, horizontalPolygon);
        return true;
    }

    private static bool TryGetBinaryVerticalFootprint(
        CadEditorViewModel editor,
        FrameNode node,
        out ImpostFootprint footprint)
    {
        footprint = default;
        if (!node.TryGetVerticalImpostAxisWorldX(out var centerX))
        {
            return false;
        }

        if (!TryResolveVerticalImpostRenderBounds(centerX, ResolveImpostThickness(node), out var left, out var right))
        {
            return false;
        }

        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, node.GetRoot())
            && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, node.GetRoot(), node, centerX, out var roundBottom, out var roundTop))
        {
            footprint = new ImpostFootprint(left, right, roundBottom, roundTop, null, null);
            return true;
        }

        var yBottom = ResolveVerticalImpostBottom(editor, node);
        var rectangularTop = ResolveVerticalImpostSpan(editor, node).Top;
        ArcSpringingGeometry? arc = null;
        if (TryResolveVerticalImpostArcTrim(editor, node, centerX, left, right, rectangularTop, out var resolvedArc))
        {
            arc = resolvedArc;
        }

        if (editor.FrameRoot?.Shape == ConstructionShape.Triangle)
        {
            var innerTri = FrameTriangleGeometry.BuildInnerTriangle(editor.FrameRoot, editor.FrameThickness);
            footprint = new ImpostFootprint(
                left,
                right,
                yBottom,
                rectangularTop,
                null,
                null,
                IsTriangular: true,
                TriBl: innerTri.Bl,
                TriBr: innerTri.Br,
                TriTop: innerTri.Top);
            return true;
        }

        footprint = new ImpostFootprint(left, right, yBottom, rectangularTop, arc, null);
        return true;
    }

    private static bool TryGetBinaryHorizontalFootprint(
        CadEditorViewModel editor,
        FrameNode node,
        out ImpostFootprint footprint)
    {
        footprint = default;
        if (!TryResolveHorizontalImpostBounds(node, out var bottom, out var top))
        {
            return false;
        }

        if (node.ChildA is null || node.ChildB is null)
        {
            return false;
        }

        var span = HorizontalImpostGeometry.ResolveRectangularSpan(editor, node);
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, node.GetRoot())
            && node.TryGetHorizontalImpostAxisWorldY(out var axisY)
            && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(editor, node.GetRoot(), node, axisY, out var circleLeft, out var circleRight))
        {
            span = (circleLeft, circleRight);
        }

        IReadOnlyList<CadPoint>? horizontalPolygon = null;
        if (HorizontalImpostGeometry.TryResolveArcAdaptedSpans(
                editor,
                node,
                bottom,
                top,
                out var leftBottom,
                out var rightBottom,
                out var leftTop,
                out var rightTop))
        {
            horizontalPolygon = HorizontalImpostGeometry.BuildFootprintPolygon(
                leftBottom,
                rightBottom,
                leftTop,
                rightTop,
                bottom,
                top);
            span = (leftBottom, rightBottom);
        }

        footprint = new ImpostFootprint(span.Left, span.Right, bottom, top, null, horizontalPolygon);
        return true;
    }

    private static CadRect ToCadRect(ImpostFootprint footprint) =>
        new(footprint.Left, footprint.Top, footprint.Right - footprint.Left, footprint.Top - footprint.Bottom);

    private static double ResolveImpostThickness(FrameNode node) =>
        node.GetResolvedImpostThicknessMm();

    private static bool TryResolveVerticalImpostRenderBounds(
        double centerX,
        double thickness,
        out double left,
        out double right)
    {
        left = right = 0;
        if (thickness <= CadMath.Epsilon)
        {
            return false;
        }

        var half = thickness / 2.0;
        left = CadMath.RoundMm(centerX - half);
        right = CadMath.RoundMm(centerX + half);
        return right - left > CadMath.Epsilon;
    }

    private static bool TryResolveHorizontalImpostRenderBounds(
        double centerY,
        double thickness,
        out double bottom,
        out double top)
    {
        bottom = top = 0;
        if (thickness <= CadMath.Epsilon)
        {
            return false;
        }

        var half = thickness / 2.0;
        bottom = CadMath.RoundMm(centerY - half);
        top = CadMath.RoundMm(centerY + half);
        return top - bottom > CadMath.Epsilon;
    }

    private static (double Bottom, double Top) ResolveVerticalImpostSpan(
        CadEditorViewModel editor,
        FrameNode splitNode,
        int uniformGapIndex = 0)
    {
        var container = splitNode.GetRoot();
        double? axisX = null;
        if (splitNode.IsUniformSplit && splitNode.SplitOrientation == SplitOrientation.Vertical)
        {
            if (splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, uniformGapIndex, out var uniformAxis))
            {
                axisX = uniformAxis;
            }
        }
        else if (splitNode.TryGetVerticalImpostAxisWorldX(out var binaryAxis))
        {
            axisX = binaryAxis;
        }

        if (axisX is double axis
            && CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, container, splitNode, axis, out var roundBottom, out var roundTop))
        {
            return (roundBottom, roundTop);
        }

        if (editor.FrameRoot is null)
        {
            return (splitNode.Y, splitNode.Y + splitNode.Height);
        }

        if (editor.FrameRoot.IsFictitiousContainer)
        {
            if (axisX is double axisWorld)
            {
                return FrameNodeRenderer.ResolveFictitiousVerticalImpostSpan(editor, splitNode, axisWorld);
            }

            return FrameNodeRenderer.ResolveFictitiousVerticalImpostSpan(
                editor,
                splitNode,
                splitNode.TryGetVerticalImpostAxisWorldX(out var fallbackAxis)
                    ? fallbackAxis
                    : splitNode.X + splitNode.Width / 2.0);
        }

        var (structuralBottom, structuralTop) = SashGeometry.ResolveVerticalImpostStructuralSpan(
            splitNode,
            editor.FrameRoot,
            editor.FrameThickness);

        var maxY = ResolveVerticalImpostExtentMaxY(editor, splitNode);
        return (
            CadMath.RoundMm(structuralBottom),
            CadMath.RoundMm(Math.Clamp(structuralTop, structuralBottom, maxY)));
    }

    private static double ResolveVerticalImpostExtentMaxY(CadEditorViewModel editor, FrameNode splitNode)
    {
        var maxY = splitNode.Y + splitNode.Height;
        var profile = editor.FrameRoot?.TopProfile;
        if (profile?.SegmentType == SegmentType.Arc &&
            profile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            maxY = Math.Max(maxY, splitNode.Y + splitNode.Height + profile.ArcHeight);
        }

        return maxY;
    }

    private static double ResolveVerticalImpostBottom(CadEditorViewModel editor, FrameNode splitNode) =>
        ResolveVerticalImpostSpan(editor, splitNode).Bottom;

    private static bool TryResolveHorizontalImpostBounds(FrameNode node, out double bottom, out double top)
    {
        bottom = top = 0;
        if (!node.TryGetHorizontalImpostAxisWorldY(out var centerY))
        {
            return false;
        }

        return TryResolveHorizontalImpostRenderBounds(centerY, ResolveImpostThickness(node), out bottom, out top);
    }

    private static bool TryResolveVerticalImpostArcTrim(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double centerX,
        double left,
        double right,
        double rectangularTop,
        out ArcSpringingGeometry arc)
    {
        arc = default;

        if (editor.FrameRoot?.TopProfile?.SegmentType != SegmentType.Arc ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(editor.FrameRoot, editor.FrameThickness, out arc))
        {
            return false;
        }

        return VerticalImpostReachesArc(editor, splitNode, centerX, left, right, rectangularTop, arc);
    }

    private static double ResolveMinInnerArcTopYBetweenX(
        ArcSpringingGeometry arc,
        double xLeft,
        double xRight,
        double minY)
    {
        var left = Math.Min(xLeft, xRight);
        var right = Math.Max(xLeft, xRight);
        var minTopY = double.MaxValue;
        var segments = Math.Clamp((int)Math.Ceiling((right - left) / 15.0), 2, 16);
        for (var i = 0; i <= segments; i++)
        {
            var x = left + (right - left) * i / segments;
            var topY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, x).Y;
            minTopY = Math.Min(minTopY, Math.Max(topY, minY));
        }

        return minTopY;
    }

    private static bool VerticalImpostReachesArc(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double centerX,
        double left,
        double right,
        double rectangularTop,
        ArcSpringingGeometry arc)
    {
        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
            splitNode,
            editor.FrameRoot!,
            editor.FrameThickness);

        if (inner.PinsFrameTop)
        {
            return true;
        }

        var chordY = editor.FrameRoot!.Y + editor.FrameRoot.Height;
        if (rectangularTop >= chordY - CadMath.MmPrecision)
        {
            return true;
        }

        var minArcTopY = ResolveMinInnerArcTopYBetweenX(arc, left, right, splitNode.Y);
        return minArcTopY > rectangularTop + CadMath.Epsilon;
    }
}
