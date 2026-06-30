namespace NurSapat.Core.Models.Orders;

public sealed class ConstructionGeometry
{
    public decimal OuterWidthMm { get; set; }

    public decimal OuterHeightMm { get; set; }

    public string DrawingJson { get; set; } = "{}";

    public List<GeometryElement> Elements { get; } = [];

    public decimal ProductAreaSqM =>
        OuterWidthMm > 0 && OuterHeightMm > 0
            ? decimal.Round(OuterWidthMm * OuterHeightMm / 1_000_000m, 4, MidpointRounding.AwayFromZero)
            : 0m;
}
