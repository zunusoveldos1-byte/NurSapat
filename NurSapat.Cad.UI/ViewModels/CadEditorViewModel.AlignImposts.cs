using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool TryAlignImpostsFromTool()
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте конструкцию";
            return false;
        }

        var targets = ResolveAlignImpostTargets();
        if (targets.Count == 0)
        {
            StatusText = "Выберите импост или ячейки для выравнивания";
            return false;
        }

        var before = CaptureConstructionMemento();
        var aligned = RecordPlacement(
            "Импосты выровнены",
            () =>
            {
                foreach (var target in targets)
                {
                    if (!TryAlignImpostTarget(target))
                    {
                        ApplyConstructionMemento(before);
                        return false;
                    }
                }

                SyncFillingsToFrameLayout();
                return true;
            });

        if (!aligned)
        {
            RefreshAfterPlacementHistoryChange();
            StatusText = "Невозможно выровнять выбранные импосты без нарушения геометрии";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        SyncLegacySelectionFromConstruction();
        StatusText = $"Выровнено групп импостов: {targets.Count}";
        return true;
    }

    private List<FrameNode> ResolveAlignImpostTargets()
    {
        var targets = new HashSet<FrameNode>();

        foreach (var item in _constructionSelection.Where(item => item.Kind == CadSelectableKind.Impost))
        {
            if (item.ImpostNode is { } node && !node.IsLeaf)
            {
                targets.Add(node);
            }
        }

        if (_selectedMullion?.SplitNode is { IsLeaf: false } selected)
        {
            targets.Add(selected);
        }

        if (targets.Count > 0)
        {
            return targets.ToList();
        }

        var leaves = new List<FrameNode>();
        foreach (var item in _constructionSelection)
        {
            if (item.ElementId is not Guid elementId)
            {
                continue;
            }

            var element = CanvasElements.FirstOrDefault(e => e.Id == elementId);
            if (element is null)
            {
                continue;
            }

            var leaf = element.Kind switch
            {
                GeometryElementKind.Glazing => SashGeometry.FindLeafForGlazing(this, element),
                GeometryElementKind.Sash => SashGeometry.FindLeafForSash(this, element),
                _ => FrameRoot?.FindLeafAt((double)element.X, (double)element.Y)
            };
            if (leaf is { IsLeaf: true })
            {
                leaves.Add(leaf);
            }
        }

        if (leaves.Count < 2 || FrameRoot is null)
        {
            return [];
        }

        foreach (var node in FrameRoot.EnumerateInternalNodes())
        {
            var children = GetSubdivisionChildren(node);
            if (children.Count < 2)
            {
                continue;
            }

            if (children.All(child => leaves.Any(leaf => IsDescendantOf(leaf, child))))
            {
                targets.Add(node);
            }
        }

        return targets.ToList();
    }

    private bool TryAlignImpostTarget(FrameNode node)
    {
        if (node.IsUniformSplit)
        {
            var inset = ResolveFrameSplitInset(node);
            return node.TryRealignUniformImposts(inset);
        }

        var children = GetSubdivisionChildren(node);
        if (children.Count < 2
            || node.SplitOrientation is not (SplitOrientation.Vertical or SplitOrientation.Horizontal))
        {
            return false;
        }

        // For binary/structural align we equalize child cell spans directly.
        var orientation = node.SplitOrientation;
        var totalSpan = children.Sum(child => GetAxisSpan(child, orientation));
        var target = CadMath.RoundMm(totalSpan / children.Count);
        var desiredSpans = Enumerable.Repeat(target, children.Count).ToArray();
        if (desiredSpans.Any(span => span <= CadMath.Epsilon))
        {
            return false;
        }

        var alreadyEqual = children
            .Select((child, index) =>
                CadMath.NearlyEqual(GetAxisSpan(child, orientation), desiredSpans[index], AlignmentToleranceMm))
            .All(equal => equal);
        if (alreadyEqual)
        {
            return true;
        }

        return ApplySubdivisionAlignmentPlan(
            new SubdivisionAlignmentPlan(node, orientation, desiredSpans));
    }
}
