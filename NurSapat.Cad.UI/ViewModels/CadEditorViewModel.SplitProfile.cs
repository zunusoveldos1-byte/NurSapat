using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private enum SplitProfileMode
    {
        SingleLeaf,
        ImpostAdjacentLeaves
    }

    private readonly record struct SplitProfileOperation(
        SplitProfileMode Mode,
        FrameNode TargetLeaf,
        FrameNode? SecondaryLeaf,
        SplitOrientation Orientation,
        bool RequiresArchCheck);

    private bool TrySplitProfileFromTool()
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте конструкцию";
            return false;
        }

        if (!TryResolveSplitProfileOperation(out var operation))
        {
            StatusText = "Выберите ячейку или профиль импоста для разбиения пополам";
            return false;
        }

        if (operation.RequiresArchCheck && HasArcTopProfile)
        {
            StatusText = "Разбиение профиля недоступно для арочной верхней кромки — используйте «Разбить арку»";
            return false;
        }

        EnsureArchFrameTopProfileForImpost();
        var impostWidth = SingleMullionPlacementWidthMm;

        if (!RecordPlacement(
                "Профиль разделён пополам",
                () =>
                {
                    if (!ApplySplitProfileOperation(operation, impostWidth))
                    {
                        return false;
                    }

                    SyncFillingsToFrameLayout();
                    return true;
                }))
        {
            RefreshAfterPlacementHistoryChange();
            StatusText = "Невозможно разделить профиль без нарушения геометрии";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        SyncLegacySelectionFromConstruction();
        SelectedElement = FrameRoot;
        StatusText = "Профиль разделён пополам";
        return true;
    }

    private bool TryResolveSplitProfileOperation(out SplitProfileOperation operation)
    {
        operation = default;
        if (FrameRoot is null)
        {
            return false;
        }

        var impostNode = _selectedMullion?.SplitNode
            ?? _constructionSelection
                .FirstOrDefault(item => item.Kind == CadSelectableKind.Impost)
                ?.ImpostNode;

        if (impostNode is
            {
                IsLeaf: false,
                IsUniformSplit: false,
                ChildA: { IsLeaf: true } childA,
                ChildB: { IsLeaf: true } childB
            })
        {
            operation = new SplitProfileOperation(
                SplitProfileMode.ImpostAdjacentLeaves,
                childA,
                childB,
                impostNode.SplitOrientation == SplitOrientation.Vertical
                    ? SplitOrientation.Horizontal
                    : SplitOrientation.Vertical,
                RequiresArchCheck: impostNode.SplitOrientation == SplitOrientation.Horizontal);
            return true;
        }

        if (SelectedElement is FrameNode { IsLeaf: true } selectedLeaf)
        {
            operation = CreateLeafSplitOperation(selectedLeaf);
            return true;
        }

        if (_constructionSelection.Count == 1
            && TryResolveLeafFromSelection(_constructionSelection.First(), out var leaf))
        {
            operation = CreateLeafSplitOperation(leaf);
            return true;
        }

        if (FrameRoot.IsLeaf)
        {
            operation = CreateLeafSplitOperation(FrameRoot);
            return true;
        }

        return false;
    }

    private SplitProfileOperation CreateLeafSplitOperation(FrameNode leaf) =>
        new(
            SplitProfileMode.SingleLeaf,
            leaf,
            null,
            leaf.Width >= leaf.Height ? SplitOrientation.Vertical : SplitOrientation.Horizontal,
            RequiresArchCheck: true);

    private bool TryResolveLeafFromSelection(CadSelectableItem item, out FrameNode leaf)
    {
        leaf = null!;
        if (FrameRoot is null)
        {
            return false;
        }

        if (item.Kind == CadSelectableKind.Impost
            && item.ImpostNode is { ChildA: { IsLeaf: true } impostChild })
        {
            leaf = impostChild;
            return true;
        }

        FrameNode? resolved = item.Kind switch
        {
            CadSelectableKind.Glazing when item.ElementId is Guid glazingId
                => CanvasElements.FirstOrDefault(e => e.Id == glazingId) is { } glazing
                    ? SashGeometry.FindLeafForGlazing(this, glazing)
                    : null,
            CadSelectableKind.Sash when item.ElementId is Guid sashId
                => CanvasElements.FirstOrDefault(e => e.Id == sashId) is { } sash
                    ? SashGeometry.FindLeafForSash(this, sash)
                    : null,
            _ => item.ElementId is Guid elementId
                && CanvasElements.FirstOrDefault(e => e.Id == elementId) is { } element
                ? FrameRoot.FindLeafAt((double)element.X, (double)element.Y)
                : null
        };

        if (resolved is { IsLeaf: true })
        {
            leaf = resolved;
            return true;
        }

        return false;
    }

    private bool ApplySplitProfileOperation(SplitProfileOperation operation, double impostWidth) =>
        operation.Mode switch
        {
            SplitProfileMode.SingleLeaf =>
                ApplyLeafHalfSplit(operation.TargetLeaf, operation.Orientation, impostWidth),
            SplitProfileMode.ImpostAdjacentLeaves when operation.SecondaryLeaf is { } secondary =>
                ApplyLeafHalfSplit(operation.TargetLeaf, operation.Orientation, impostWidth)
                && ApplyLeafHalfSplit(secondary, operation.Orientation, impostWidth),
            _ => false
        };

    private bool ApplyLeafHalfSplit(FrameNode leaf, SplitOrientation orientation, double impostWidth)
    {
        var split = orientation == SplitOrientation.Vertical
            ? leaf.TrySplitVerticalHalf(ImpostLineStyle.Solid, impostWidth)
            : leaf.TrySplitHorizontalHalf(ImpostLineStyle.Solid, impostWidth);
        if (!split)
        {
            return false;
        }

        TrackImpostSplit(leaf);
        return true;
    }
}
