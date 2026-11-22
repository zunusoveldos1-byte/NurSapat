using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    /// <summary>
    /// «Разбить профиль пополам»: физически делит выбранную рейку коробки
    /// (стык 90° посередине внешней длины). Не вставляет импост в проём.
    /// </summary>
    private bool TrySplitProfileFromTool()
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте конструкцию";
            return false;
        }

        if (!TryGetSelectedFrameProfileSegment(out var rail, out var segmentIndex))
        {
            StatusText = "Выберите профиль рамы (верх/низ/лево/право) для разбиения пополам";
            return false;
        }

        if (rail == FrameProfileRail.Top && HasArcTopProfile)
        {
            StatusText = "Разбиение арочного верхнего профиля недоступно — используйте «Разбить арку»";
            return false;
        }

        if (!RecordPlacement(
                "Профиль разделён пополам",
                () => FrameRoot.TrySplitProfileRailSegmentInHalf(rail, segmentIndex)))
        {
            RefreshAfterPlacementHistoryChange();
            StatusText = "Невозможно разделить выбранный сегмент (слишком короткая рейка)";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        OnPropertyChanged(nameof(FrameRoot));
        RequestSceneRefresh();
        StatusText = $"Сегмент профиля разделён пополам ({DescribeFrameProfileRail(rail)})";
        return true;
    }

    private bool TryGetSelectedFrameProfileSegment(out FrameProfileRail rail, out int segmentIndex)
    {
        rail = default;
        segmentIndex = 0;
        var selected = _constructionSelection.FirstOrDefault(item =>
            item.Kind == CadSelectableKind.Frame && item.ProfileRail is not null);
        if (selected?.ProfileRail is not FrameProfileRail profileRail)
        {
            return false;
        }

        rail = profileRail;
        segmentIndex = selected.ProfileSegmentIndex ?? 0;
        return true;
    }
}
