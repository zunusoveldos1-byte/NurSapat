using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

/// <summary>Вид стыка на профиле коробки (рейке).</summary>
public enum FrameRailJointKind
{
    /// <summary>Прямой рез 90° поперёк рейки (стык двух половин профиля).</summary>
    Butt90 = 0
}

/// <summary>
/// Узел стыка на рейке рамы (физическое деление профиля, не импост в проёме).
/// </summary>
public sealed class FrameProfileRailJoint
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public FrameProfileRail Rail { get; init; }

    /// <summary>
    /// Доля длины вдоль внешней кромки OuterStart → OuterEnd (0..1).
    /// 0.5 — деление рейки пополам.
    /// </summary>
    public double RelativeT { get; set; } = 0.5;

    /// <summary>
    /// Смещение стыка от исходной оси рейки вдоль её нормали.
    /// Вместе с RelativeT позволяет свободно перемещать стык в 2D.
    /// </summary>
    public double NormalOffsetMm { get; set; }

    /// <summary>Центральный стык-конёк сохраняет мировую координату X при перемещении.</summary>
    public bool LockHorizontalAxis { get; set; }

    /// <summary>
    /// Зафиксированная позиция стыка в локальных координатах корневой рамы.
    /// Не зависит от последующей деформации крайних углов.
    /// </summary>
    public bool HasFixedAnchor { get; set; }
    public double AnchorLocalX { get; set; }
    public double AnchorLocalY { get; set; }

    public FrameRailJointKind Kind { get; set; } = FrameRailJointKind.Butt90;
}

/// <summary>Снимок стыка рейки для undo/redo.</summary>
public sealed class FrameProfileRailJointSnapshot
{
    public Guid Id { get; init; }
    public FrameProfileRail Rail { get; init; }
    public double RelativeT { get; init; }
    public double NormalOffsetMm { get; init; }
    public bool LockHorizontalAxis { get; init; }
    public bool HasFixedAnchor { get; init; }
    public double AnchorLocalX { get; init; }
    public double AnchorLocalY { get; init; }
    public FrameRailJointKind Kind { get; init; }
}
