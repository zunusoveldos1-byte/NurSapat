using NurSapat.Cad.Core.Geometry;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Крайний импост (стойка/ригель) внутри светового проёма без деления ячейки пополам.
/// </summary>
public sealed class ExtremeImpostSlot
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public bool IsExtreme => true;

    public SplitOrientation Orientation { get; init; }

    public ImpostEdgeAlignment EdgeAlignment { get; set; }

    /// <summary>Дополнительный отступ от внутренней кромки проёма (мм), по умолчанию 0.</summary>
    public double OffsetMm { get; set; }

    /// <summary>Нижняя/левая граница пролёта по осям сетки (мм).</summary>
    public double SpanStartMm { get; set; }

    /// <summary>Верхняя/правая граница пролёта по осям сетки (мм).</summary>
    public double SpanEndMm { get; set; }

    public bool HasResolvedSpan => SpanEndMm > SpanStartMm + CadMath.Epsilon;

    public double ThicknessMm { get; set; } = FrameNode.DefaultImpostProfileMm;

    /// <summary>Вычитание из светового проёма на примыкающем крае (мм), напр. 4 для нащельника.</summary>
    public double LightOpeningInsetMm { get; set; }
}
