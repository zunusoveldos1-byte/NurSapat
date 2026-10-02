using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private const double AlignmentToleranceMm = 1.0;

    private static IReadOnlyList<FrameNode> GetSubdivisionChildren(FrameNode node)
    {
        if (node.IsUniformSplit)
        {
            return node.UniformCells;
        }

        return node.ChildA is not null && node.ChildB is not null
            ? [node.ChildA, node.ChildB]
            : [];
    }

    private static bool IsDescendantOf(FrameNode leaf, FrameNode ancestor)
    {
        for (FrameNode? current = leaf; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LeafSpansChildAxis(
        FrameNode leaf,
        FrameNode child,
        SplitOrientation orientation) =>
        orientation == SplitOrientation.Vertical
            ? CadMath.NearlyEqual(leaf.X, child.X, AlignmentToleranceMm)
              && CadMath.NearlyEqual(leaf.Width, child.Width, AlignmentToleranceMm)
            : CadMath.NearlyEqual(leaf.Y, child.Y, AlignmentToleranceMm)
              && CadMath.NearlyEqual(leaf.Height, child.Height, AlignmentToleranceMm);

    private static double GetAxisSpan(FrameNode node, SplitOrientation orientation) =>
        orientation == SplitOrientation.Vertical ? node.Width : node.Height;

    private static bool HaveEqualDimension(IEnumerable<double> values)
    {
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return true;
        }

        var reference = enumerator.Current;
        while (enumerator.MoveNext())
        {
            if (!CadMath.NearlyEqual(reference, enumerator.Current, AlignmentToleranceMm))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ApplySubdivisionAlignmentPlan(SubdivisionAlignmentPlan plan)
    {
        var node = plan.Node;
        var halfImpost = node.GetResolvedImpostThicknessMm() / 2.0;

        if (!node.IsUniformSplit)
        {
            if (node.ChildA is null || plan.DesiredChildSpans.Count != 2)
            {
                return false;
            }

            return plan.Orientation == SplitOrientation.Vertical
                ? node.TrySetVerticalImpostAxisWorldX(
                    node.ChildA.X + plan.DesiredChildSpans[0] + halfImpost)
                : node.TrySetHorizontalImpostAxisWorldY(
                    node.ChildA.Y + plan.DesiredChildSpans[0] + halfImpost);
        }

        var cells = node.UniformCells;
        if (cells.Count != plan.DesiredChildSpans.Count)
        {
            return false;
        }

        for (var gap = 0; gap < cells.Count - 1; gap++)
        {
            var axis = plan.Orientation == SplitOrientation.Vertical
                ? cells[gap].X + plan.DesiredChildSpans[gap] + halfImpost
                : cells[gap].Y + plan.DesiredChildSpans[gap] + halfImpost;
            var moved = plan.Orientation == SplitOrientation.Vertical
                ? node.TrySetUniformVerticalImpostGapAxisWorldX(gap, axis)
                : node.TrySetUniformHorizontalImpostGapAxisWorldY(gap, axis);
            if (!moved)
            {
                return false;
            }
        }

        return true;
    }

    private sealed record SubdivisionAlignmentPlan(
        FrameNode Node,
        SplitOrientation Orientation,
        IReadOnlyList<double> DesiredChildSpans);
}
