using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Rendering;

internal static class HorizontalImpostGeometry
{
    public static (double Left, double Right) ResolveRectangularSpan(
        CadEditorViewModel editor,
        FrameNode splitNode)
    {
        if (editor.FrameRoot is null)
        {
            return (splitNode.X, splitNode.X + splitNode.Width);
        }

        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
            splitNode,
            editor.FrameRoot,
            editor.FrameThickness);

        if (inner.Width <= CadMath.Epsilon)
        {
            return (splitNode.X, splitNode.X + splitNode.Width);
        }

        return (
            CadMath.RoundMm(Math.Clamp(inner.Left, splitNode.X, splitNode.X + splitNode.Width)),
            CadMath.RoundMm(Math.Clamp(inner.Right, splitNode.X, splitNode.X + splitNode.Width)));
    }

    public static (double Left, double Right) ResolveSpanAtY(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double worldY)
    {
        var rectangular = ResolveRectangularSpan(editor, splitNode);
        if (editor.FrameRoot?.TopProfile?.SegmentType != SegmentType.Arc ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(editor.FrameRoot, editor.FrameThickness, out var arc))
        {
            return rectangular;
        }

        if (!FrameProfileGeometry.TryResolveInnerArcHorizontalSpanAtY(
                arc,
                worldY,
                rectangular.Left,
                rectangular.Right,
                out var leftX,
                out var rightX))
        {
            return rectangular;
        }

        return (leftX, rightX);
    }

    public static bool TryResolveArcAdaptedSpans(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double bottom,
        double top,
        out double leftBottom,
        out double rightBottom,
        out double leftTop,
        out double rightTop)
    {
        leftBottom = rightBottom = leftTop = rightTop = 0;
        var rectangular = ResolveRectangularSpan(editor, splitNode);
        (leftBottom, rightBottom) = ResolveSpanAtY(editor, splitNode, bottom);
        (leftTop, rightTop) = ResolveSpanAtY(editor, splitNode, top);

        var isAdapted =
            Math.Abs(leftBottom - leftTop) > CadMath.Epsilon ||
            Math.Abs(rightBottom - rightTop) > CadMath.Epsilon ||
            leftBottom > rectangular.Left + CadMath.Epsilon ||
            rightBottom < rectangular.Right - CadMath.Epsilon;

        return isAdapted;
    }

    public static List<CadPoint> BuildFootprintPolygon(
        double leftBottom,
        double rightBottom,
        double leftTop,
        double rightTop,
        double bottom,
        double top)
    {
        return
        [
            new(leftBottom, bottom),
            new(rightBottom, bottom),
            new(rightTop, top),
            new(leftTop, top)
        ];
    }

    public static bool ContainsPoint(
        IReadOnlyList<CadPoint> polygon,
        CadPoint point)
    {
        return ProfileBandHitTest.ContainsPointInPolygon(polygon, point);
    }

    public static bool IntersectsRect(IReadOnlyList<CadPoint> polygon, CadRect selection) =>
        ProfileBandHitTest.RectIntersectsPolygon(polygon, selection);

    public static double ResolveNodeExtentMaxY(CadEditorViewModel editor, FrameNode splitNode)
    {
        var maxY = splitNode.Y + splitNode.Height;
        var profile = editor.FrameRoot?.TopProfile;
        if (profile?.SegmentType == SegmentType.Arc &&
            profile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon &&
            editor.FrameRoot is not null)
        {
            var chordY = editor.FrameRoot.Y + editor.FrameRoot.Height;
            maxY = Math.Max(maxY, chordY + profile.ArcHeight);
        }

        return maxY;
    }

    public static double ResolveMaxCenterAxisY(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double halfThickness,
        double dragInsetMm)
    {
        const double minCellMm = 40;
        var extentMaxY = ResolveNodeExtentMaxY(editor, splitNode) - minCellMm - halfThickness;

        if (editor.FrameRoot is null)
        {
            return extentMaxY;
        }

        var rectangular = ResolveRectangularSpan(editor, splitNode);
        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
            splitNode,
            editor.FrameRoot,
            editor.FrameThickness);

        var innerTop = inner.Top;
        if (editor.FrameRoot.TopProfile?.SegmentType == SegmentType.Arc &&
            SashGeometry.TryResolveArcAwareFrameInnerTopForSpan(
                editor.FrameRoot,
                editor.FrameThickness,
                rectangular.Left,
                rectangular.Right,
                innerTop,
                out var arcAwareTop))
        {
            innerTop = arcAwareTop;
        }

        var innerLimit = innerTop - dragInsetMm - halfThickness;
        var maxY = Math.Max(innerLimit, extentMaxY);

        if (ResolveMaxDragAxisY(editor, splitNode, halfThickness) is double arcCenterLimit)
        {
            maxY = Math.Max(maxY, Math.Min(extentMaxY, arcCenterLimit));
        }

        return Math.Min(extentMaxY, maxY);
    }

    public static double? ResolveMaxDragAxisY(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double halfThickness)
    {
        if (editor.FrameRoot?.TopProfile?.SegmentType != SegmentType.Arc ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(editor.FrameRoot, editor.FrameThickness, out var arc))
        {
            return null;
        }

        var minSpan = Math.Max(splitNode.GetResolvedImpostThicknessMm(), 40.0);
        var minHalfSpan = minSpan / 2.0;
        if (arc.InnerRadius <= minHalfSpan + CadMath.Epsilon)
        {
            return null;
        }

        var underRoot = arc.InnerRadius * arc.InnerRadius - minHalfSpan * minHalfSpan;
        var maxTopY = arc.Center.Y + Math.Sqrt(Math.Max(0, underRoot));
        var apexY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, arc.Center.X).Y;
        maxTopY = Math.Min(maxTopY, apexY);
        var maxCenterY = maxTopY - halfThickness;

        if (maxCenterY <= arc.ChordY + halfThickness + CadMath.MmPrecision)
        {
            return null;
        }

        return CadMath.RoundMm(maxCenterY);
    }
}
