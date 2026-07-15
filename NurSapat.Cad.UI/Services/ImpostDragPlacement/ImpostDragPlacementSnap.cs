using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.UI.Rendering;

namespace NurSapat.Cad.UI.Services.ImpostDragPlacement;

/// <summary>
/// Привязка оси импоста к абсолютным GridLines и внутренним границам рамы.
/// </summary>
internal static class ImpostDragPlacementSnap
{
    public const double SnapThresholdMm = 8.0;

    public static double ResolveVerticalAxis(
        FrameNode root,
        FrameNode leaf,
        SashGeometry.InnerOpeningBounds inner,
        double rawAxisX,
        double minAxisX,
        double maxAxisX)
    {
        var candidates = CollectSnapCandidates(
            root,
            GridLineOrientation.Vertical,
            (inner.Left + inner.Right) / 2.0,
            minAxisX,
            maxAxisX);
        AddInnerFrameVerticalEdges(leaf, candidates, minAxisX, maxAxisX);
        var clamped = Math.Clamp(rawAxisX, minAxisX, maxAxisX);
        var snapped = SnapToNearest(clamped, candidates, SnapThresholdMm);
        return CadMath.RoundMm(snapped ?? clamped);
    }

    public static double ResolveHorizontalAxis(
        FrameNode root,
        FrameNode leaf,
        SashGeometry.InnerOpeningBounds inner,
        double rawAxisY,
        double minAxisY,
        double maxAxisY)
    {
        var candidates = CollectSnapCandidates(
            root,
            GridLineOrientation.Horizontal,
            (inner.Bottom + inner.Top) / 2.0,
            minAxisY,
            maxAxisY);
        AddInnerFrameHorizontalEdges(leaf, candidates, minAxisY, maxAxisY);
        var clamped = Math.Clamp(rawAxisY, minAxisY, maxAxisY);
        var snapped = SnapToNearest(clamped, candidates, SnapThresholdMm);
        return CadMath.RoundMm(snapped ?? clamped);
    }

    private static List<double> CollectSnapCandidates(
        FrameNode root,
        GridLineOrientation orientation,
        double midpoint,
        double minAxis,
        double maxAxis)
    {
        var candidates = new List<double> { midpoint };
        var grid = FrameNodeGridLineProjector.Project(root);
        foreach (var line in grid.OfOrientation(orientation))
        {
            if (line.Coordinate >= minAxis - CadMath.Epsilon && line.Coordinate <= maxAxis + CadMath.Epsilon)
            {
                candidates.Add(line.Coordinate);
            }
        }

        return candidates;
    }

    private static double? SnapToNearest(double value, IReadOnlyList<double> candidates, double thresholdMm)
    {
        double? best = null;
        var bestDistance = thresholdMm;
        foreach (var candidate in candidates)
        {
            var distance = Math.Abs(value - candidate);
            if (distance <= bestDistance + CadMath.Epsilon)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private static void AddInnerFrameVerticalEdges(
        FrameNode leaf,
        List<double> candidates,
        double minAxisX,
        double maxAxisX)
    {
        var edge = leaf.ResolveEdgeLightOpeningInsets();
        var left = leaf.X + edge.Left;
        var right = leaf.X + leaf.Width - edge.Right;
        if (left >= minAxisX && left <= maxAxisX)
        {
            candidates.Add(left);
        }

        if (right >= minAxisX && right <= maxAxisX)
        {
            candidates.Add(right);
        }
    }

    private static void AddInnerFrameHorizontalEdges(
        FrameNode leaf,
        List<double> candidates,
        double minAxisY,
        double maxAxisY)
    {
        var edge = leaf.ResolveEdgeLightOpeningInsets();
        var bottom = leaf.Y + edge.Bottom;
        var top = leaf.Y + leaf.Height - edge.Top;
        if (bottom >= minAxisY && bottom <= maxAxisY)
        {
            candidates.Add(bottom);
        }

        if (top >= minAxisY && top <= maxAxisY)
        {
            candidates.Add(top);
        }
    }
}
