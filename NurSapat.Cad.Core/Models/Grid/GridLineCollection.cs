namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>Единая коллекция всех осей сетки (вертикальных и горизонтальных).</summary>
public sealed class GridLineCollection
{
    public static GridLineCollection Empty { get; } = new(Array.Empty<GridLine>());

    private readonly IReadOnlyList<GridLine> _all;

    public GridLineCollection(IReadOnlyList<GridLine> lines)
    {
        _all = lines ?? Array.Empty<GridLine>();
    }

    public IReadOnlyList<GridLine> All => _all;

    public IEnumerable<GridLine> OfOrientation(GridLineOrientation orientation)
    {
        foreach (var line in _all)
        {
            if (line.Orientation == orientation)
            {
                yield return line;
            }
        }
    }

    public GridLine? FindById(GridLineId id)
    {
        foreach (var line in _all)
        {
            if (line.Id.Value == id.Value)
            {
                return line;
            }
        }

        return null;
    }

    public GridLine? FindBySource(Guid nodeId, int? uniformGapIndex)
    {
        foreach (var line in _all)
        {
            if (line.SourceNodeId != nodeId)
            {
                continue;
            }

            if (uniformGapIndex is null)
            {
                if (line.UniformGapIndex is null)
                {
                    return line;
                }

                continue;
            }

            if (line.UniformGapIndex == uniformGapIndex)
            {
                return line;
            }
        }

        return null;
    }
}
