using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    /// <summary>
    /// Делит все ячейки, пересекаемые одной горизонтальной осью. Каждая ячейка
    /// получает собственный узел разбиения: это сохраняет общую стартовую высоту
    /// ригеля, но позволяет затем перемещать сегменты независимо.
    /// </summary>
    private bool TrySplitSingleHorizontalImpost(
        FrameNode cell,
        double axisWorldY,
        ImpostLineStyle lineStyle,
        double impostWidthMm)
    {
        var localAxisY = axisWorldY - cell.Y;
        if (!cell.CanSplitHorizontalAt(localAxisY, lineStyle, impostWidthMm)
            || !cell.TrySplitHorizontalAt(localAxisY, lineStyle, impostWidthMm))
        {
            return false;
        }

        TrackImpostSplit(cell);
        return true;
    }

}
