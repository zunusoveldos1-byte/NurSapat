using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private ExtremeImpostHoverPreview? _extremeImpostHoverPreview;
    private (FrameNode Leaf, Guid SlotId)? _selectedExtremeImpost;

    public ExtremeImpostHoverPreview? ExtremeImpostHoverPreview => _extremeImpostHoverPreview;

    public bool IsExtremeImpostToolActive => CurrentToolMode == WindowToolMode.ExtremeImpost;

    public bool IsExtremeImpostEditPanelActive =>
        WorkspaceMode == CadEditorWorkspaceMode.Editing && _selectedExtremeImpost is not null;

    public IReadOnlyList<ExtremeImpostPlacementOption> ExtremeImpostPlacementOptions { get; } =
    [
        new(ExtremeImpostPlacementKind.Post, "Стойка (вертикальная)"),
        new(ExtremeImpostPlacementKind.Transom, "Ригель (горизонтальный)")
    ];

    [ObservableProperty]
    private ExtremeImpostPlacementOption? _selectedExtremeImpostPlacementOption;

    [ObservableProperty]
    private double _extremeImpostOffsetMm;

    private bool _isSyncingExtremeImpostPanel;

    public void UpdateExtremeImpostHover(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.ExtremeImpost || FrameRoot is null)
        {
            ClearExtremeImpostHover();
            return;
        }

        var placement = SelectedExtremeImpostPlacementOption?.Kind ?? ExtremeImpostPlacementKind.Post;
        var thickness = GetImpostProfileWidth(WindowToolMode.RealVerticalImpost);
        if (!ExtremeImpostGeometry.TryHitTestHover(
                FrameRoot,
                FrameThickness,
                worldPoint,
                placement,
                thickness,
                out var preview))
        {
            ClearExtremeImpostHover();
            return;
        }

        if (_extremeImpostHoverPreview?.Leaf == preview.Leaf
            && _extremeImpostHoverPreview.Edge == preview.Edge)
        {
            return;
        }

        _extremeImpostHoverPreview = preview;
        RequestSceneRefresh();
    }

    public void ClearExtremeImpostHover()
    {
        if (_extremeImpostHoverPreview is null)
        {
            return;
        }

        _extremeImpostHoverPreview = null;
        RequestSceneRefresh();
    }

    public bool TryHandleExtremeImpostClick(CadPoint worldPoint)
    {
        if (WorkspaceMode == CadEditorWorkspaceMode.Editing)
        {
            return TrySelectExtremeImpostAt(worldPoint);
        }

        if (WorkspaceMode != CadEditorWorkspaceMode.Creation
            || CurrentToolMode != WindowToolMode.ExtremeImpost
            || FrameRoot is null)
        {
            return false;
        }

        var placement = SelectedExtremeImpostPlacementOption?.Kind ?? ExtremeImpostPlacementKind.Post;
        var thickness = GetImpostProfileWidth(WindowToolMode.RealVerticalImpost);
        if (!ExtremeImpostGeometry.TryHitTestHover(
                FrameRoot,
                FrameThickness,
                worldPoint,
                placement,
                thickness,
                out var preview))
        {
            StatusText = "Наведите курсор на край светового проёма";
            return false;
        }

        var orientation = preview.Orientation;
        return RecordPlacement(
            "Крайний импост установлен",
            () =>
            {
                if (!preview.Leaf.TryUpsertExtremeImpost(orientation, preview.Edge, thickness))
                {
                    return false;
                }

                SyncExtremeImpostCanvasElements();
                SyncFillingsToFrameLayout();
                return true;
            });
    }

    private bool TrySelectExtremeImpostAt(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        foreach (var (leaf, slot) in FrameRoot.EnumerateExtremeImposts())
        {
            if (!TryHitExtremeImpost(leaf, slot, worldPoint))
            {
                continue;
            }

            _selectedExtremeImpost = (leaf, slot.Id);
            _isSyncingExtremeImpostPanel = true;
            ExtremeImpostOffsetMm = slot.OffsetMm;
            _isSyncingExtremeImpostPanel = false;
            _constructionSelection.Clear();
            _constructionSelection.Add(CadSelectableItem.ExtremeImpost(leaf, slot.Id));
            OnPropertyChanged(nameof(ConstructionSelection));
            OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
            StatusText = $"Крайний импост: отступ {slot.OffsetMm:F0} мм";
            RequestSceneRefresh();
            return true;
        }

        return false;
    }

    private bool TryHitExtremeImpost(FrameNode leaf, ExtremeImpostSlot slot, CadPoint worldPoint) =>
        FrameRoot is not null
        && ExtremeImpostGeometry.ContainsCommittedImpost(FrameRoot, FrameThickness, leaf, slot, worldPoint);

    internal void SyncExtremeImpostSelectionFromConstruction(CadSelectableItem item)
    {
        if (item.Kind != CadSelectableKind.ExtremeImpost
            || item.ImpostNode is not FrameNode leaf
            || item.ElementId is not Guid slotId)
        {
            return;
        }

        var slot = leaf.FindExtremeImpost(slotId);
        if (slot is null)
        {
            return;
        }

        _selectedExtremeImpost = (leaf, slotId);
        _isSyncingExtremeImpostPanel = true;
        ExtremeImpostOffsetMm = slot.OffsetMm;
        _isSyncingExtremeImpostPanel = false;
        OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
    }

    partial void OnExtremeImpostOffsetMmChanged(double value)
    {
        if (_isSyncingExtremeImpostPanel || _selectedExtremeImpost is not { } selected)
        {
            return;
        }

        if (value < 0)
        {
            return;
        }

        if (!selected.Leaf.TrySetExtremeImpostOffset(selected.SlotId, value))
        {
            return;
        }

        SyncExtremeImpostCanvasElements();
        SyncFillingsToFrameLayout();
        RequestSceneRefresh();
        StatusText = $"Крайний импост: отступ {value:F0} мм";
    }

    partial void OnSelectedExtremeImpostPlacementOptionChanged(ExtremeImpostPlacementOption? value)
    {
        if (value is null)
        {
            SelectedExtremeImpostPlacementOption = ExtremeImpostPlacementOptions[0];
        }

        ClearExtremeImpostHover();
        OnPropertyChanged(nameof(IsExtremeImpostToolActive));
    }

    internal void SyncExtremeImpostCanvasElements()
    {
        if (FrameRoot is null)
        {
            return;
        }

        var activeSlotIds = new HashSet<Guid>();
        foreach (var (leaf, slot) in FrameRoot.EnumerateExtremeImposts())
        {
            activeSlotIds.Add(slot.Id);
            var element = CanvasElements.FirstOrDefault(e =>
                e.Kind == GeometryElementKind.EdgeMullion && e.ExtremeImpostSlotId == slot.Id);

            if (element is null)
            {
                element = new GeometryElement
                {
                    Kind = GeometryElementKind.EdgeMullion,
                    Label = "Стойка/ригель в крайнем положении",
                    SourceToolId = "mullion-edge",
                    IsExtremeImpost = true,
                    ExtremeImpostSlotId = slot.Id,
                    HostFrameNodeId = leaf.NodeId
                };
                AddCanvasElement(element);
            }

            UpdateExtremeImpostElementGeometry(leaf, slot, element);
        }

        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind == GeometryElementKind.EdgeMullion
                && element.IsExtremeImpost
                && element.ExtremeImpostSlotId is Guid slotId
                && !activeSlotIds.Contains(slotId))
            {
                CanvasElements.RemoveAt(i);
            }
        }

        RefreshConstructionVolume();
    }

    private void UpdateExtremeImpostElementGeometry(FrameNode leaf, ExtremeImpostSlot slot, GeometryElement element)
    {
        if (FrameRoot is null)
        {
            return;
        }

        var inner = Rendering.SashGeometry.ResolveLeafInnerOpeningBounds(leaf, FrameRoot, FrameThickness, includeExtremeImposts: false);
        var axis = ExtremeImpostGeometry.ResolveAxisWorld(inner, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
        ExtremeImpostGeometry.ResolveSpan(inner, slot.EdgeAlignment, out var spanStart, out var spanEnd);
        element.EdgeAlignment = slot.EdgeAlignment;
        element.ExtremeImpostOffsetMm = slot.OffsetMm;
        element.HostFrameNodeId = leaf.NodeId;

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            var length = spanEnd - spanStart;
            element.X = (decimal)axis;
            element.Y = (decimal)((spanStart + spanEnd) / 2.0);
            element.WidthMm = (decimal)slot.ThicknessMm;
            element.HeightMm = (decimal)length;
            element.EndX = (decimal)axis;
            element.EndY = (decimal)spanEnd;
            return;
        }

        var horizontalLength = spanEnd - spanStart;
        element.X = (decimal)((spanStart + spanEnd) / 2.0);
        element.Y = (decimal)axis;
        element.WidthMm = (decimal)horizontalLength;
        element.HeightMm = (decimal)slot.ThicknessMm;
        element.EndX = (decimal)spanEnd;
        element.EndY = (decimal)axis;
    }

    private void InitializeExtremeImpostTool()
    {
        SelectedExtremeImpostPlacementOption ??= ExtremeImpostPlacementOptions[0];
        ClearExtremeImpostHover();
        OnPropertyChanged(nameof(IsExtremeImpostToolActive));
    }

    private void ClearExtremeImpostSelection()
    {
        _selectedExtremeImpost = null;
        _isSyncingExtremeImpostPanel = true;
        ExtremeImpostOffsetMm = 0;
        _isSyncingExtremeImpostPanel = false;
        OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
    }
}

public sealed record ExtremeImpostPlacementOption(ExtremeImpostPlacementKind Kind, string Label);
