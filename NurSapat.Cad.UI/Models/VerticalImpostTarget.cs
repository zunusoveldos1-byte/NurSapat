using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Models;

/// <summary>
/// Ссылка на вертикальный импост: бинарный узел или зазор uniform-разбиения.
/// </summary>
public sealed class VerticalImpostTarget
{
    public required FrameNode SplitNode { get; init; }

    /// <summary>Индекс зазора для uniform-разбиения; для бинарного импоста — null.</summary>
    public int? UniformGapIndex { get; init; }

    public bool Matches(FrameNode node, int uniformGapIndex) =>
        ReferenceEquals(SplitNode, node) &&
        (UniformGapIndex ?? 0) == uniformGapIndex;
}
