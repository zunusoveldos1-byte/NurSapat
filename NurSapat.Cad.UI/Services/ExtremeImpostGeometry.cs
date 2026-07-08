using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

public enum ExtremeImpostPlacementKind
{
    Post,
    Transom
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
    private const double EdgeHitBandMm = 90;

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

        var inner = SashGeometry.ResolveLeafInnerOpeningBounds(leaf, frameRoot, frameThickness, includeExtremeImposts: false);
        if (inner.Width <= CadMath.Epsilon || inner.Height <= CadMath.Epsilon)
        {
            return false;
        }

        if (worldPoint.X < inner.Left - CadMath.MmPrecision
            || worldPoint.X > inner.Right + CadMath.MmPrecision
            || worldPoint.Y < inner.Bottom - CadMath.MmPrecision
            || worldPoint.Y > inner.Top + CadMath.MmPrecision)
        {
            return false;
        }

        if (!TryResolveNearestEdge(
                worldPoint,
                inner,
                placementKind,
                out var edge,
                out var distance))
        {
            return false;
        }

        if (distance > EdgeHitBandMm)
        {
            return false;
        }

        var orientation = edge is ImpostEdgeAlignment.Left or ImpostEdgeAlignment.Right
            ? SplitOrientation.Vertical
            : SplitOrientation.Horizontal;

        if (placementKind == ExtremeImpostPlacementKind.Post
            && orientation != SplitOrientation.Vertical)
        {
            return false;
        }

        if (placementKind == ExtremeImpostPlacementKind.Transom
            && orientation != SplitOrientation.Horizontal)
        {
            return false;
        }

        var axis = ResolveAxisWorld(inner, edge, thicknessMm, 0);
        ResolveSpan(inner, edge, out var spanStart, out var spanEnd);
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

    public static void ResolveSpan(
        in SashGeometry.InnerOpeningBounds inner,
        ImpostEdgeAlignment edge,
        out double spanStart,
        out double spanEnd)
    {
        if (edge is ImpostEdgeAlignment.Left or ImpostEdgeAlignment.Right)
        {
            spanStart = inner.Bottom;
            spanEnd = inner.Top;
            return;
        }

        spanStart = inner.Left;
        spanEnd = inner.Right;
    }

    public static void ApplyExtremeImpostInsets(
        FrameNode leaf,
        ref SashGeometry.InnerOpeningBounds inner)
    {
        foreach (var slot in leaf.ExtremeImposts)
        {
            var inset = slot.ThicknessMm + slot.OffsetMm;
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

        var inner = SashGeometry.ResolveLeafInnerOpeningBounds(leaf, frameRoot, frameThickness, includeExtremeImposts: false);
        var axis = ResolveAxisWorld(inner, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
        var half = slot.ThicknessMm / 2.0;

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            return worldPoint.X >= axis - half - toleranceMm
                   && worldPoint.X <= axis + half + toleranceMm
                   && worldPoint.Y >= inner.Bottom - toleranceMm
                   && worldPoint.Y <= inner.Top + toleranceMm;
        }

        return worldPoint.Y >= axis - half - toleranceMm
               && worldPoint.Y <= axis + half + toleranceMm
               && worldPoint.X >= inner.Left - toleranceMm
               && worldPoint.X <= inner.Right + toleranceMm;
    }

    private static bool TryResolveNearestEdge(
        CadPoint worldPoint,
        in SashGeometry.InnerOpeningBounds inner,
        ExtremeImpostPlacementKind placementKind,
        out ImpostEdgeAlignment edge,
        out double distance)
    {
        edge = ImpostEdgeAlignment.Left;
        distance = double.MaxValue;

        if (placementKind == ExtremeImpostPlacementKind.Post)
        {
            var leftDist = worldPoint.X - inner.Left;
            var rightDist = inner.Right - worldPoint.X;
            if (leftDist <= rightDist)
            {
                edge = ImpostEdgeAlignment.Left;
                distance = leftDist;
            }
            else
            {
                edge = ImpostEdgeAlignment.Right;
                distance = rightDist;
            }

            return distance >= 0;
        }

        var bottomDist = worldPoint.Y - inner.Bottom;
        var topDist = inner.Top - worldPoint.Y;
        if (bottomDist <= topDist)
        {
            edge = ImpostEdgeAlignment.Bottom;
            distance = bottomDist;
        }
        else
        {
            edge = ImpostEdgeAlignment.Top;
            distance = topDist;
        }

        return distance >= 0;
    }
}
