using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services.ImpostDragPlacement;

namespace NurSapat.Cad.UI.ViewModels;

public partial class CadEditorViewModel
{
    private readonly ImpostDragPlacementController _impostDragPlacement = new();

    public ImpostDragPlacementPreview? ImpostDragPlacementPreview => _impostDragPlacement.Preview;

    public bool IsImpostDragPlacementActive => _impostDragPlacement.IsPointerDown;

    public bool IsImpostDragPlacementPreviewVisible => _impostDragPlacement.Preview is not null;

    public bool IsImpostDragPlacementTool(WindowToolMode mode) =>
        ImpostDragPlacementController.IsDragPlacementTool(mode);

    private ImpostDragPlacementSession CreateImpostDragPlacementSession() =>
        new(
            FrameRoot,
            FrameThickness,
            CurrentToolMode,
            GetImpostProfileWidth(CurrentToolMode),
            GetImpostLineStyle(CurrentToolMode),
            SnapWorldPoint);

    public bool TryBeginImpostDragPlacement(CadPoint worldPoint, double screenX, double screenY)
    {
        if (WorkspaceMode != CadEditorWorkspaceMode.Creation
            || !IsImpostDragPlacementTool(CurrentToolMode)
            || FrameRoot is null)
        {
            return false;
        }

        if (!_impostDragPlacement.TryBegin(CreateImpostDragPlacementSession(), worldPoint, screenX, screenY))
        {
            StatusText = FrameRoot is null
                ? "Сначала создайте коробку"
                : "Кликните внутри свободной ячейки";
            return false;
        }

        OnPropertyChanged(nameof(ImpostDragPlacementPreview));
        OnPropertyChanged(nameof(IsImpostDragPlacementPreviewVisible));
        OnPropertyChanged(nameof(IsImpostDragPlacementActive));
        StatusText = CurrentToolMode == WindowToolMode.RealVerticalImpost
            ? "Потяните вверх или вниз, затем перемещайте по горизонтали"
            : "Потяните влево или вправо, затем перемещайте по вертикали";
        return true;
    }

    public void UpdateImpostDragPlacement(CadPoint worldPoint, double screenX, double screenY)
    {
        if (!_impostDragPlacement.IsPointerDown)
        {
            return;
        }

        _impostDragPlacement.Update(CreateImpostDragPlacementSession(), worldPoint, screenX, screenY);
        OnPropertyChanged(nameof(ImpostDragPlacementPreview));
        OnPropertyChanged(nameof(IsImpostDragPlacementPreviewVisible));

        if (_impostDragPlacement.Preview is { } preview)
        {
            StatusText = preview.IsValid
                ? "Отпустите кнопку мыши для фиксации импоста"
                : "Недопустимое положение импоста";
        }
    }

    public bool CommitImpostDragPlacement()
    {
        if (!_impostDragPlacement.TryGetCommitAxis(out var leaf, out var axisWorld))
        {
            CancelImpostDragPlacement();
            return false;
        }

        var mode = CurrentToolMode;
        var width = GetImpostProfileWidth(mode);
        var style = GetImpostLineStyle(mode);
        EnsureArchFrameTopProfileForImpost();

        var committed = RecordPlacement("Импост установлен", () =>
        {
            var split = mode == WindowToolMode.RealVerticalImpost
                ? leaf.TrySplitVerticalAt(axisWorld - leaf.X, style, width)
                : leaf.TrySplitHorizontalAt(axisWorld - leaf.Y, style, width);

            if (!split)
            {
                return false;
            }

            TrackImpostSplit(leaf);
            SelectedElement = FrameRoot;
            OnPropertyChanged(nameof(FrameRoot));
            return true;
        });

        _impostDragPlacement.EndDrag();
        OnPropertyChanged(nameof(ImpostDragPlacementPreview));
        OnPropertyChanged(nameof(IsImpostDragPlacementPreviewVisible));
        OnPropertyChanged(nameof(IsImpostDragPlacementActive));

        if (committed)
        {
            RefreshAfterImpostAdded(mode);
            StatusText = mode == WindowToolMode.RealVerticalImpost
                ? "Импост вертикальный: зажмите ЛКМ в ячейке и сделайте короткий жест"
                : "Импост горизонтальный: зажмите ЛКМ в ячейке и сделайте короткий жест";
        }
        else
        {
            StatusText = "Невозможно установить импост в этой ячейке";
        }

        return committed;
    }

    public void CancelImpostDragPlacement()
    {
        if (!_impostDragPlacement.IsPointerDown)
        {
            return;
        }

        _impostDragPlacement.Cancel();
        OnPropertyChanged(nameof(ImpostDragPlacementPreview));
        OnPropertyChanged(nameof(IsImpostDragPlacementPreviewVisible));
        OnPropertyChanged(nameof(IsImpostDragPlacementActive));
    }
}
