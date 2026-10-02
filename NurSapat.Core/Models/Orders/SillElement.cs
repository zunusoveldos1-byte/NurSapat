namespace NurSapat.Core.Models.Orders;

/// <summary>Подоконник, прикреплённый к нижнему профилю корневой рамы снаружи.</summary>
public sealed class SillElement
{
    public const double StandardThicknessMm = 30;
    public const double DefaultSideOverhangMm = 40;
    public const string DefaultColorHex = "#ECEFF1";
    public static readonly Guid DefaultMaterialId = Guid.Parse("b1000084-0000-4000-8000-000000000084");

    public Guid Id { get; init; } = Guid.NewGuid();

    public double LeftOverhangMm { get; set; } = DefaultSideOverhangMm;

    public double RightOverhangMm { get; set; } = DefaultSideOverhangMm;

    /// <summary>Ширина по горизонтали (мм), включая выпуски.</summary>
    public double WidthMm { get; set; }

    /// <summary>Глубина выступа наружу (мм) — для спецификации.</summary>
    public double DepthMm { get; set; } = 200;

    /// <summary>Толщина плашки в 2D-виде (мм).</summary>
    public double ThicknessMm { get; set; } = StandardThicknessMm;

    public Guid? MaterialId { get; set; } = DefaultMaterialId;

    /// <summary>Цвет отрисовки (hex), по умолчанию белый алюминий.</summary>
    public string Color { get; set; } = DefaultColorHex;

    public double ResolveWidthForFrame(double frameWidthMm) =>
        frameWidthMm + LeftOverhangMm + RightOverhangMm;
}
