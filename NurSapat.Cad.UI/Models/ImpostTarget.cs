using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.UI.Models;

/// <summary>
/// Ссылка на импост / ось сетки (вертикальная или горизонтальная — один тип).
/// </summary>
public sealed class ImpostTarget
{
    public required FrameNode SplitNode { get; init; }

    /// <summary>Индекс зазора для uniform-разбиения; для бинарного импоста — null.</summary>
    public int? UniformGapIndex { get; init; }

    public GridLineId? GridLineId { get; init; }

    public GridLineOrientation Orientation { get; init; }

    public bool Matches(FrameNode node, int uniformGapIndex) =>
        ReferenceEquals(SplitNode, node) &&
        (UniformGapIndex ?? 0) == uniformGapIndex;
}
