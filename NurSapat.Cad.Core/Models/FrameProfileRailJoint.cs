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

    public FrameRailJointKind Kind { get; set; } = FrameRailJointKind.Butt90;
}

/// <summary>Снимок стыка рейки для undo/redo.</summary>
public sealed class FrameProfileRailJointSnapshot
{
    public Guid Id { get; init; }
    public FrameProfileRail Rail { get; init; }
    public double RelativeT { get; init; }
    public FrameRailJointKind Kind { get; init; }
}
