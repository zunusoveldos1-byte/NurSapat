using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool TryAlignGlazingUnitsFromTool()
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте конструкцию";
            return false;
        }

        var selectedGlazingIds = ResolveSelectedGlazingIds();
        if (selectedGlazingIds.Count < 2)
        {
            StatusText = "Выберите не менее двух стеклопакетов";
            return false;
        }

        if (!TryMeasureSelectedGlazing(selectedGlazingIds, out var measurements))
        {
            StatusText = "Не удалось определить проёмы выбранных стеклопакетов";
            return false;
        }

        if (HaveEqualDimension(measurements.Select(item => item.GlassWidth))
            && HaveEqualDimension(measurements.Select(item => item.GlassHeight)))
        {
            StatusText = "Выбранные стеклопакеты уже имеют одинаковый размер";
            return true;
        }

        var plans = BuildLeafSpanAlignmentPlans(
            measurements.Select(item => new LeafSpanMeasurement(item.Leaf, item.GlassWidth, item.GlassHeight)).ToList(),
            useStructuralCellSpan: false);
        if (plans.Count == 0)
        {
            StatusText = "Выбранные стеклопакеты не разделены общими импостами";
            return false;
        }

        var before = CaptureConstructionMemento();
        var aligned = RecordPlacement(
            "Стеклопакеты выровнены",
            () =>
            {
                foreach (var plan in plans)
                {
                    if (!ApplySubdivisionAlignmentPlan(plan))
                    {
                        ApplyConstructionMemento(before);
                        return false;
                    }
                }

                SyncFillingsToFrameLayout();
                if (!TryMeasureSelectedGlazing(selectedGlazingIds, out var updated)
                    || !HaveEqualDimension(updated.Select(item => item.GlassWidth))
                    || !HaveEqualDimension(updated.Select(item => item.GlassHeight)))
                {
                    ApplyConstructionMemento(before);
                    return false;
                }

                return true;
            });

        if (!aligned)
        {
            RefreshAfterPlacementHistoryChange();
            StatusText = "Невозможно выровнять выбранные стеклопакеты без нарушения геометрии";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        SyncLegacySelectionFromConstruction();
        StatusText = $"Выровнено стеклопакетов: {measurements.Count}";
        return true;
    }

    private HashSet<Guid> ResolveSelectedGlazingIds()
    {
        var result = _constructionSelection
            .Where(item => item.Kind == CadSelectableKind.Glazing && item.ElementId.HasValue)
            .Select(item => item.ElementId!.Value)
            .ToHashSet();

        var selectedSashIds = _constructionSelection
            .Where(item => item.Kind == CadSelectableKind.Sash && item.ElementId.HasValue)
            .Select(item => item.ElementId!.Value)
            .ToHashSet();

        if (selectedSashIds.Count == 0)
        {
            return result;
        }

        foreach (var glazing in CanvasElements.Where(element =>
                     element.Kind == GeometryElementKind.Glazing
                     && element.HostSashId is Guid sashId
                     && selectedSashIds.Contains(sashId)))
        {
            result.Add(glazing.Id);
        }

        return result;
    }

    private bool TryMeasureSelectedGlazing(
        IReadOnlySet<Guid> glazingIds,
        out List<SelectedGlazingMeasurement> measurements)
    {
        measurements = [];
        var occupiedLeaves = new HashSet<FrameNode>();

        foreach (var glazingId in glazingIds)
        {
            var glazing = CanvasElements.FirstOrDefault(element =>
                element.Id == glazingId && element.Kind == GeometryElementKind.Glazing);
            if (glazing is null
                || SashGeometry.FindLeafForGlazing(this, glazing) is not { IsLeaf: true } leaf
                || !occupiedLeaves.Add(leaf)
                || !SashGeometry.TryResolveGlassForGlazingElement(
                    this,
                    glazing,
                    CanvasElements,
                    out var glass)
                || glass.Width <= CadMath.Epsilon
                || glass.Height <= CadMath.Epsilon)
            {
                measurements = [];
                return false;
            }

            measurements.Add(new SelectedGlazingMeasurement(
                glazing.Id,
                leaf,
                glass.Width,
                glass.Height));
        }

        return measurements.Count == glazingIds.Count;
    }

    private List<SubdivisionAlignmentPlan> BuildLeafSpanAlignmentPlans(
        IReadOnlyList<LeafSpanMeasurement> measurements,
        bool useStructuralCellSpan)
    {
        var plans = new List<SubdivisionAlignmentPlan>();
        if (FrameRoot is null)
        {
            return plans;
        }

        foreach (var node in FrameRoot.EnumerateInternalNodes())
        {
            if (node.SplitOrientation is not (SplitOrientation.Vertical or SplitOrientation.Horizontal))
            {
                continue;
            }

            var children = GetSubdivisionChildren(node);
            if (children.Count < 2)
            {
                continue;
            }

            var selectedByChild = children
                .Select(child => measurements.Where(item => IsDescendantOf(item.Leaf, child)).ToList())
                .ToList();
            if (selectedByChild.Any(group => group.Count == 0))
            {
                continue;
            }

            var orientation = node.SplitOrientation;
            if (!ChildrenCanBeAlignedByMeasuredSpan(children, selectedByChild, orientation))
            {
                continue;
            }

            double[] desiredSpans;
            if (useStructuralCellSpan)
            {
                var totalSpan = children.Sum(child => GetAxisSpan(child, orientation));
                var target = totalSpan / children.Count;
                desiredSpans = Enumerable.Repeat(CadMath.RoundMm(target), children.Count).ToArray();
            }
            else
            {
                var offsets = new double[children.Count];
                var totalSpan = 0.0;
                for (var i = 0; i < children.Count; i++)
                {
                    var span = GetAxisSpan(children[i], orientation);
                    var measuredSpan = GetMeasuredAxisSpan(selectedByChild[i][0], orientation);
                    offsets[i] = span - measuredSpan;
                    totalSpan += span;
                }

                var targetMeasuredSpan = (totalSpan - offsets.Sum()) / children.Count;
                if (targetMeasuredSpan <= CadMath.Epsilon)
                {
                    continue;
                }

                desiredSpans = offsets
                    .Select(offset => CadMath.RoundMm(targetMeasuredSpan + offset))
                    .ToArray();
            }

            if (desiredSpans.Any(span => span <= CadMath.Epsilon))
            {
                continue;
            }

            var requiresChange = children
                .Select((child, index) =>
                    !CadMath.NearlyEqual(
                        GetAxisSpan(child, orientation),
                        desiredSpans[index],
                        AlignmentToleranceMm))
                .Any(changed => changed);
            if (requiresChange)
            {
                plans.Add(new SubdivisionAlignmentPlan(node, orientation, desiredSpans));
            }
        }

        return plans;
    }

    private static bool ChildrenCanBeAlignedByMeasuredSpan(
        IReadOnlyList<FrameNode> children,
        IReadOnlyList<List<LeafSpanMeasurement>> selectedByChild,
        SplitOrientation orientation)
    {
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var group = selectedByChild[i];
            var referenceSpan = GetMeasuredAxisSpan(group[0], orientation);

            foreach (var item in group)
            {
                if (!LeafSpansChildAxis(item.Leaf, child, orientation)
                    || !CadMath.NearlyEqual(
                        referenceSpan,
                        GetMeasuredAxisSpan(item, orientation),
                        AlignmentToleranceMm))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static double GetMeasuredAxisSpan(
        LeafSpanMeasurement measurement,
        SplitOrientation orientation) =>
        orientation == SplitOrientation.Vertical
            ? measurement.MeasuredWidth
            : measurement.MeasuredHeight;

    private sealed record SelectedGlazingMeasurement(
        Guid GlazingId,
        FrameNode Leaf,
        double GlassWidth,
        double GlassHeight);

    private sealed record LeafSpanMeasurement(
        FrameNode Leaf,
        double MeasuredWidth,
        double MeasuredHeight);
}
