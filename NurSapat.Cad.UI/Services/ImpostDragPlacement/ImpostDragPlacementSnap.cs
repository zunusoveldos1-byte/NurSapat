using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;

namespace NurSapat.Cad.UI.Services.ImpostDragPlacement;

/// <summary>
/// Привязка оси импоста к центру, существующим импостам и внутренним границам рамы.
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
        var candidates = CollectVerticalSnapCandidates(root, leaf, inner, minAxisX, maxAxisX);
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
        var candidates = CollectHorizontalSnapCandidates(root, leaf, inner, minAxisY, maxAxisY);
        var clamped = Math.Clamp(rawAxisY, minAxisY, maxAxisY);
        var snapped = SnapToNearest(clamped, candidates, SnapThresholdMm);
        return CadMath.RoundMm(snapped ?? clamped);
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

    private static List<double> CollectVerticalSnapCandidates(
        FrameNode root,
        FrameNode leaf,
        SashGeometry.InnerOpeningBounds inner,
        double minAxisX,
        double maxAxisX)
    {
        var candidates = new List<double> { (inner.Left + inner.Right) / 2.0 };

        foreach (var axis in CollectVerticalImpostAxes(root))
        {
            if (axis >= minAxisX - CadMath.Epsilon && axis <= maxAxisX + CadMath.Epsilon)
            {
                candidates.Add(axis);
            }
        }

        AddInnerFrameVerticalEdges(leaf, candidates, minAxisX, maxAxisX);
        return candidates;
    }

    private static List<double> CollectHorizontalSnapCandidates(
        FrameNode root,
        FrameNode leaf,
        SashGeometry.InnerOpeningBounds inner,
        double minAxisY,
        double maxAxisY)
    {
        var candidates = new List<double> { (inner.Bottom + inner.Top) / 2.0 };

        foreach (var axis in CollectHorizontalImpostAxes(root))
        {
            if (axis >= minAxisY - CadMath.Epsilon && axis <= maxAxisY + CadMath.Epsilon)
            {
                candidates.Add(axis);
            }
        }

        AddInnerFrameHorizontalEdges(leaf, candidates, minAxisY, maxAxisY);
        return candidates;
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

    private static IEnumerable<double> CollectVerticalImpostAxes(FrameNode root)
    {
        foreach (var node in root.EnumerateInternalNodes())
        {
            if (node.IsUniformSplit && node.SplitOrientation == SplitOrientation.Vertical)
            {
                var cells = node.UniformCells;
                for (var i = 0; i < cells.Count - 1; i++)
                {
                    if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var axis))
                    {
                        yield return axis;
                    }
                }

                continue;
            }

            if (node.SplitOrientation == SplitOrientation.Vertical
                && node.TryGetVerticalImpostAxisWorldX(out var axisX))
            {
                yield return axisX;
            }
        }
    }

    private static IEnumerable<double> CollectHorizontalImpostAxes(FrameNode root)
    {
        foreach (var node in root.EnumerateInternalNodes())
        {
            if (node.IsUniformSplit && node.SplitOrientation == SplitOrientation.Horizontal)
            {
                var cells = node.UniformCells;
                for (var i = 0; i < cells.Count - 1; i++)
                {
                    if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var axis))
                    {
                        yield return axis;
                    }
                }

                continue;
            }

            if (node.SplitOrientation == SplitOrientation.Horizontal
                && node.TryGetHorizontalImpostAxisWorldY(out var axisY))
            {
                yield return axisY;
            }
        }
    }
}
