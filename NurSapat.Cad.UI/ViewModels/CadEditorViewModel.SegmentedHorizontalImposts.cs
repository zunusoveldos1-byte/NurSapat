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

    private bool TrySplitDoubleHorizontalImpostIntoSegments(
        FrameNode sourceLeaf,
        double axisWorldY,
        double impostWidthMm,
        ImpostLineStyle lineStyle)
    {
        var cells = ResolveHorizontalSegmentCells(sourceLeaf, axisWorldY);
        if (cells.Count == 0 || cells.Any(cell =>
                !cell.CanSplitHorizontalAt(axisWorldY - cell.Y, lineStyle, impostWidthMm)))
        {
            return false;
        }

        foreach (var cell in cells)
        {
            if (!cell.TrySplitHorizontalAt(axisWorldY - cell.Y, lineStyle, impostWidthMm))
            {
                return false;
            }

            TrackImpostSplit(cell);
        }

        return true;
    }

    private IReadOnlyList<FrameNode> ResolveHorizontalSegmentCells(FrameNode sourceLeaf, double axisWorldY)
    {
        var root = FrameRoot ?? sourceLeaf.GetRoot();
        var cells = root.EnumerateTerminalLeaves()
            .Where(cell =>
                axisWorldY > cell.Y + CadMath.MmPrecision &&
                axisWorldY < cell.Y + FrameNode.ResolveHorizontalSplitExtentLocal(cell, root) - CadMath.MmPrecision)
            .OrderBy(cell => cell.X)
            .ToList();

        // Нельзя потерять ячейку, в которой пользователь начал действие,
        // например у арочного верха с расширенной вертикальной областью.
        if (!cells.Contains(sourceLeaf))
        {
            cells.Add(sourceLeaf);
            cells.Sort((left, right) => left.X.CompareTo(right.X));
        }

        return cells;
    }
}
