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

    public double ThicknessMm { get; set; } = FrameNode.DefaultImpostProfileMm;
}
