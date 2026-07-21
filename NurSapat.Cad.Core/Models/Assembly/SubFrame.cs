using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Независимая подрама с локальной системой координат и собственным деревом FrameNode.
/// </summary>
public sealed class SubFrame
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Мировое смещение начала локальной системы (нижний левый угол зоны).</summary>
    public double OriginWorldX { get; set; }

    public double OriginWorldY { get; set; }

    /// <summary>Внешний габарит зоны (display), без уменьшения нащельником.</summary>
    public double OuterWidth { get; set; }

    public double OuterHeight { get; set; }

    /// <summary>
    /// Нащельник/стыковой зазор на границе с переходным профилем (мм).
    /// Уменьшает только внутреннюю математику, не outer width.
    /// </summary>
    public double TransitionCoverStripMm { get; init; }

    /// <summary>Нащельник на противоположной внешней грани (если есть).</summary>
    public double OuterCoverStripMm { get; init; }

    public required FrameNode Root { get; init; }

    public int OrderIndex { get; set; }

    public double OuterRightWorld => OriginWorldX + OuterWidth;

    public double OuterTopWorld => OriginWorldY + OuterHeight;

    /// <summary>Внутренняя ширина для деления импостов и световых расчётов.</summary>
    public double InternalWidth =>
        CadMath.RoundMm(Math.Max(0, OuterWidth - TransitionCoverStripMm - OuterCoverStripMm));

    /// <summary>Внутренняя высота для деления.</summary>
    public double InternalHeight =>
        CadMath.RoundMm(Math.Max(0, OuterHeight));

    public bool ContainsWorld(double worldX, double worldY) =>
        worldX >= OriginWorldX - CadMath.MmPrecision
        && worldX <= OuterRightWorld + CadMath.MmPrecision
        && worldY >= OriginWorldY - CadMath.MmPrecision
        && worldY <= OuterTopWorld + CadMath.MmPrecision;

    public double WorldToLocalX(double worldX) => CadMath.RoundMm(worldX - OriginWorldX);

    public double WorldToLocalY(double worldY) => CadMath.RoundMm(worldY - OriginWorldY);

    public double LocalToWorldX(double localX) => CadMath.RoundMm(OriginWorldX + localX);

    public double LocalToWorldY(double localY) => CadMath.RoundMm(OriginWorldY + localY);
}
