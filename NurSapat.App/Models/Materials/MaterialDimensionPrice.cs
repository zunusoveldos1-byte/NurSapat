namespace NurSapat.App.Models.Materials;

public sealed class MaterialDimensionPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public decimal Price { get; set; }
}
