namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>
/// Математическая ось сетки в абсолютных координатах контейнера.
/// Не является профилем: толщина 0 для Virtual; Physical хранит толщину для граней Face±Thickness/2.
/// </summary>
public sealed class GridLine
{
    public required GridLineId Id { get; init; }

    public required GridLineOrientation Orientation { get; init; }

    /// <summary>Абсолютная координата оси: X для Vertical, Y для Horizontal.</summary>
    public required double Coordinate { get; init; }

    public required GridLineKind Kind { get; init; }

    /// <summary>Толщина профиля мм; для Virtual всегда 0.</summary>
    public required double ThicknessMm { get; init; }

    /// <summary>Происхождение в дереве FrameNode (не используется в математике осей).</summary>
    public Guid? SourceNodeId { get; init; }

    /// <summary>Индекс зазора uniform-сплита; null для бинарного узла.</summary>
    public int? UniformGapIndex { get; init; }

    public double Face1 => Coordinate - ThicknessMm / 2.0;

    public double Face2 => Coordinate + ThicknessMm / 2.0;
}
