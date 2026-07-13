using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

public enum ExtremeImpostPlacementKind
{
    Post,
    Transom,

    /// <summary>
    /// Автоопределение ориентации магнитным прилипанием к ближайшей оси
    /// конструкции (используется «умными» накладными профилями — нащельник).
    /// </summary>
    Auto
}

/// <summary>Подсветка края проёма при наведении в режиме крайнего импоста.</summary>
public sealed class ExtremeImpostHoverPreview
{
    public required FrameNode Leaf { get; init; }
    public required ImpostEdgeAlignment Edge { get; init; }
    public required SplitOrientation Orientation { get; init; }
    public required double AxisWorld { get; init; }
    public required double SpanStart { get; init; }
    public required double SpanEnd { get; init; }
    public required double ThicknessMm { get; init; }

    public bool IsVertical =>
        Orientation == SplitOrientation.Vertical;
}

internal static class ExtremeImpostGeometry
{
    public static bool TryHitTestHover(
        FrameNode? frameRoot,
        double frameThickness,
        CadPoint worldPoint,
        ExtremeImpostPlacementKind placementKind,
        double thicknessMm,
        out ExtremeImpostHoverPreview preview)
    {
        preview = null!;
        if (frameRoot is null || thicknessMm <= CadMath.Epsilon)
        {
            return false;
        }

        var leaf = frameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf)
        {
            return false;
        }

        var cellAxes = SashGeometry.ResolveLeafCellAxisBoundsAtPoint(
            leaf,
            frameRoot,
            frameThickness,
            worldPoint.X,
            worldPoint.Y);
        if (cellAxes.Width <= CadMath.Epsilon || cellAxes.Height <= CadMath.Epsilon)
        {
            return false;
        }

        return TryBuildMagneticPreview(
            frameRoot,
            frameThickness,
            leaf,
            cellAxes,
            worldPoint,
            placementKind,
            thicknessMm,
            out preview);
    }

    private static bool TryBuildMagneticPreview(
        FrameNode frameRoot,
        double frameThickness,
        FrameNode leaf,
        in SashGeometry.InnerOpeningBounds cellAxes,
        CadPoint worldPoint,
        ExtremeImpostPlacementKind placementKind,
        double thicknessMm,
        out ExtremeImpostHoverPreview preview)
    {
        preview = null!;

        if (!TryResolveNearestEdgeInCell(cellAxes, worldPoint, placementKind, out var edge, out var orientation))
        {
            return false;
        }

        var axis = ResolveAxisWorld(cellAxes, edge, thicknessMm, 0);
        ResolveSpan(leaf, frameRoot, frameThickness, cellAxes, edge, thicknessMm, out var spanStart, out var spanEnd);
        if (spanEnd - spanStart <= CadMath.Epsilon)
        {
            return false;
        }

        preview = new ExtremeImpostHoverPreview
        {
            Leaf = leaf,
            Edge = edge,
            Orientation = orientation,
            AxisWorld = axis,
            SpanStart = spanStart,
            SpanEnd = spanEnd,
            ThicknessMm = thicknessMm
        };
        return true;
    }

    private static bool TryResolveNearestEdgeInCell(
        in SashGeometry.InnerOpeningBounds cellAxes,
        CadPoint worldPoint,
        ExtremeImpostPlacementKind placementKind,
        out ImpostEdgeAlignment edge,
        out SplitOrientation orientation)
    {
        edge = ImpostEdgeAlignment.Left;
        orientation = SplitOrientation.Vertical;

        var distLeft = Math.Abs(worldPoint.X - cellAxes.Left);
        var distRight = Math.Abs(cellAxes.Right - worldPoint.X);
        var distBottom = Math.Abs(worldPoint.Y - cellAxes.Bottom);
        var distTop = cellAxes.IsTriangular
            ? Math.Abs(worldPoint.Y - cellAxes.ResolveTopAtX(worldPoint.X))
            : Math.Abs(cellAxes.Top - worldPoint.Y);

        switch (placementKind)
        {
            case ExtremeImpostPlacementKind.Post:
                orientation = SplitOrientation.Vertical;
                edge = distLeft <= distRight ? ImpostEdgeAlignment.Left : ImpostEdgeAlignment.Right;
                return true;
            case ExtremeImpostPlacementKind.Transom:
                orientation = SplitOrientation.Horizontal;
                edge = distBottom <= distTop ? ImpostEdgeAlignment.Bottom : ImpostEdgeAlignment.Top;
                return true;
            default:
            {
                var verticalEdge = distLeft <= distRight
                    ? ImpostEdgeAlignment.Left
                    : ImpostEdgeAlignment.Right;
                var horizontalEdge = distBottom <= distTop
                    ? ImpostEdgeAlignment.Bottom
                    : ImpostEdgeAlignment.Top;
                var deltaX = distLeft <= distRight ? distLeft : distRight;
                var deltaY = distBottom <= distTop ? distBottom : distTop;
                edge = deltaX <= deltaY ? verticalEdge : horizontalEdge;
                orientation = edge is ImpostEdgeAlignment.Left or ImpostEdgeAlignment.Right
                    ? SplitOrientation.Vertical
                    : SplitOrientation.Horizontal;
                return true;
            }
        }
    }

    public static double ResolveAxisWorld(
        in SashGeometry.InnerOpeningBounds inner,
        ImpostEdgeAlignment edge,
        double thicknessMm,
        double offsetMm)
    {
        var half = thicknessMm / 2.0;
        offsetMm = Math.Max(0, offsetMm);
        return edge switch
        {
            ImpostEdgeAlignment.Left => CadMath.RoundMm(inner.Left + offsetMm + half),
            ImpostEdgeAlignment.Right => CadMath.RoundMm(inner.Right - offsetMm - half),
            ImpostEdgeAlignment.Bottom => CadMath.RoundMm(inner.Bottom + offsetMm + half),
            ImpostEdgeAlignment.Top => CadMath.RoundMm(inner.Top - offsetMm - half),
            _ => 0
        };
    }

    /// <summary>
    /// Пролёт крайнего импоста в пределах квадранта сетки под курсором.
    /// </summary>
    public static void ResolveSpan(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        in SashGeometry.InnerOpeningBounds cellAxes,
        ImpostEdgeAlignment edge,
        double profileThicknessMm,
        out double spanStart,
        out double spanEnd,
        ExtremeImpostSlot? slot = null)
    {
        if (slot is { HasResolvedSpan: true })
        {
            spanStart = slot.SpanStartMm;
            spanEnd = slot.SpanEndMm;
            return;
        }

        var useGridAxes = frameRoot?.IsFictitiousContainer == true;

        if (edge is ImpostEdgeAlignment.Left or ImpostEdgeAlignment.Right)
        {
            spanStart = cellAxes.Bottom;
            spanEnd = cellAxes.Top;

            if (!useGridAxes && frameRoot is not null)
            {
                SnapVerticalSpanToProfileContacts(leaf, frameRoot, frameThickness, cellAxes, ref spanStart, ref spanEnd);
            }

            ClipVerticalSpanAtPerpendicularProfiles(leaf, cellAxes, useAxisLines: useGridAxes, ref spanStart, ref spanEnd);
        }
        else
        {
            spanStart = cellAxes.Left;
            spanEnd = cellAxes.Right;

            if (!useGridAxes && frameRoot is not null)
            {
                SnapHorizontalSpanToProfileContacts(leaf, frameRoot, frameThickness, cellAxes, ref spanStart, ref spanEnd);
            }

            ClipHorizontalSpanAtPerpendicularProfiles(leaf, cellAxes, useAxisLines: useGridAxes, ref spanStart, ref spanEnd);
        }

        spanStart = CadMath.RoundMm(spanStart);
        spanEnd = CadMath.RoundMm(Math.Max(spanStart, spanEnd));
    }

    private static void SnapVerticalSpanToProfileContacts(
        FrameNode leaf,
        FrameNode frameRoot,
        double frameThickness,
        in SashGeometry.InnerOpeningBounds inner,
        ref double spanStart,
        ref double spanEnd)
    {
        if (inner.PinsFrameBottom)
        {
            var inset = FrameProfileGeometry.ResolveInnerInset(
                frameRoot.Width,
                frameRoot.Height,
                frameThickness);
            spanStart = CadMath.RoundMm(frameRoot.Y + inset);
        }

        if (inner.PinsFrameTop)
        {
            if (frameRoot.Shape == ConstructionShape.Triangle
                && SashGeometry.TryResolveTriangleInnerTopForSpan(
                    frameRoot,
                    frameThickness,
                    inner.Left,
                    inner.Right,
                    out var triangleTop))
            {
                spanEnd = triangleTop;
            }
            else
            {
                var inset = FrameProfileGeometry.ResolveInnerInset(
                    frameRoot.Width,
                    frameRoot.Height,
                    frameThickness);
                var flatTop = CadMath.RoundMm(frameRoot.Y + frameRoot.Height - inset);
                if (SashGeometry.TryResolveArcAwareFrameInnerTopForSpan(
                        frameRoot,
                        frameThickness,
                        inner.Left,
                        inner.Right,
                        flatTop,
                        out var arcTop))
                {
                    spanEnd = arcTop;
                }
                else
                {
                    spanEnd = flatTop;
                }
            }
        }
    }

    private static void SnapHorizontalSpanToProfileContacts(
        FrameNode leaf,
        FrameNode frameRoot,
        double frameThickness,
        in SashGeometry.InnerOpeningBounds inner,
        ref double spanStart,
        ref double spanEnd)
    {
        if (inner.PinsFrameLeft)
        {
            var inset = FrameProfileGeometry.ResolveInnerInset(
                frameRoot.Width,
                frameRoot.Height,
                frameThickness);
            spanStart = CadMath.RoundMm(frameRoot.X + inset);
        }

        if (inner.PinsFrameRight)
        {
            var inset = FrameProfileGeometry.ResolveInnerInset(
                frameRoot.Width,
                frameRoot.Height,
                frameThickness);
            spanEnd = CadMath.RoundMm(frameRoot.X + frameRoot.Width - inset);
        }
    }

    private static void ClipVerticalSpanAtPerpendicularProfiles(
        FrameNode leaf,
        in SashGeometry.InnerOpeningBounds inner,
        bool useAxisLines,
        ref double spanStart,
        ref double spanEnd)
    {
        foreach (var slot in leaf.ExtremeImposts)
        {
            if (slot.Orientation != SplitOrientation.Horizontal)
            {
                continue;
            }

            var axis = ResolveAxisWorld(inner, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
            if (useAxisLines)
            {
                if (slot.EdgeAlignment == ImpostEdgeAlignment.Bottom
                    && axis > spanStart + CadMath.MmPrecision
                    && axis < spanEnd - CadMath.MmPrecision)
                {
                    spanStart = Math.Max(spanStart, axis);
                }
                else if (slot.EdgeAlignment == ImpostEdgeAlignment.Top
                         && axis > spanStart + CadMath.MmPrecision
                         && axis < spanEnd - CadMath.MmPrecision)
                {
                    spanEnd = Math.Min(spanEnd, axis);
                }

                continue;
            }

            var half = slot.ThicknessMm / 2.0;
            if (slot.EdgeAlignment == ImpostEdgeAlignment.Bottom
                && axis + half > spanStart + CadMath.MmPrecision
                && axis - half < spanEnd - CadMath.MmPrecision)
            {
                spanStart = Math.Max(spanStart, axis + half);
            }
            else if (slot.EdgeAlignment == ImpostEdgeAlignment.Top
                     && axis + half > spanStart + CadMath.MmPrecision
                     && axis - half < spanEnd - CadMath.MmPrecision)
            {
                spanEnd = Math.Min(spanEnd, axis - half);
            }
        }
    }

    private static void ClipHorizontalSpanAtPerpendicularProfiles(
        FrameNode leaf,
        in SashGeometry.InnerOpeningBounds inner,
        bool useAxisLines,
        ref double spanStart,
        ref double spanEnd)
    {
        foreach (var slot in leaf.ExtremeImposts)
        {
            if (slot.Orientation != SplitOrientation.Vertical)
            {
                continue;
            }

            var axis = ResolveAxisWorld(inner, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
            if (useAxisLines)
            {
                if (slot.EdgeAlignment == ImpostEdgeAlignment.Left
                    && axis > spanStart + CadMath.MmPrecision
                    && axis < spanEnd - CadMath.MmPrecision)
                {
                    spanStart = Math.Max(spanStart, axis);
                }
                else if (slot.EdgeAlignment == ImpostEdgeAlignment.Right
                         && axis > spanStart + CadMath.MmPrecision
                         && axis < spanEnd - CadMath.MmPrecision)
                {
                    spanEnd = Math.Min(spanEnd, axis);
                }

                continue;
            }

            var half = slot.ThicknessMm / 2.0;
            if (slot.EdgeAlignment == ImpostEdgeAlignment.Left
                && axis + half > spanStart + CadMath.MmPrecision
                && axis - half < spanEnd - CadMath.MmPrecision)
            {
                spanStart = Math.Max(spanStart, axis + half);
            }
            else if (slot.EdgeAlignment == ImpostEdgeAlignment.Right
                     && axis + half > spanStart + CadMath.MmPrecision
                     && axis - half < spanEnd - CadMath.MmPrecision)
            {
                spanEnd = Math.Min(spanEnd, axis - half);
            }
        }
    }

    public static void ApplyExtremeImpostInsets(
        FrameNode leaf,
        ref SashGeometry.InnerOpeningBounds inner)
    {
        foreach (var slot in leaf.ExtremeImposts)
        {
            var inset = slot.LightOpeningInsetMm > CadMath.Epsilon
                ? slot.LightOpeningInsetMm
                : slot.ThicknessMm;
            inset += slot.OffsetMm;
            switch (slot.EdgeAlignment)
            {
                case ImpostEdgeAlignment.Left:
                    inner.Left = CadMath.RoundMm(inner.Left + inset);
                    inner.PinsFrameLeft = false;
                    break;
                case ImpostEdgeAlignment.Right:
                    inner.Right = CadMath.RoundMm(inner.Right - inset);
                    inner.PinsFrameRight = false;
                    break;
                case ImpostEdgeAlignment.Bottom:
                    inner.Bottom = CadMath.RoundMm(inner.Bottom + inset);
                    inner.PinsFrameBottom = false;
                    break;
                case ImpostEdgeAlignment.Top:
                    inner.Top = CadMath.RoundMm(inner.Top - inset);
                    inner.PinsFrameTop = false;
                    break;
            }
        }
    }

    public static bool ContainsCommittedImpost(
        FrameNode? frameRoot,
        double frameThickness,
        FrameNode leaf,
        ExtremeImpostSlot slot,
        CadPoint worldPoint,
        double toleranceMm = 20)
    {
        if (frameRoot is null)
        {
            return false;
        }

        var cellAxes = slot.HasResolvedSpan
            ? SashGeometry.ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness)
            : SashGeometry.ResolveLeafCellAxisBoundsAtPoint(
                leaf,
                frameRoot,
                frameThickness,
                worldPoint.X,
                worldPoint.Y);
        var axis = ResolveAxisWorld(cellAxes, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
        var half = slot.ThicknessMm / 2.0;
        ResolveSpan(leaf, frameRoot, frameThickness, cellAxes, slot.EdgeAlignment, slot.ThicknessMm, out var spanStart, out var spanEnd, slot);

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            return worldPoint.X >= axis - half - toleranceMm
                   && worldPoint.X <= axis + half + toleranceMm
                   && worldPoint.Y >= spanStart - toleranceMm
                   && worldPoint.Y <= spanEnd + toleranceMm;
        }

        return worldPoint.Y >= axis - half - toleranceMm
               && worldPoint.Y <= axis + half + toleranceMm
               && worldPoint.X >= spanStart - toleranceMm
               && worldPoint.X <= spanEnd + toleranceMm;
    }
}
