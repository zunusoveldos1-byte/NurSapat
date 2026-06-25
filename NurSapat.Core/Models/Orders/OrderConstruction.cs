using NurSapat.Core.Models.Calculation;

namespace NurSapat.Core.Models.Orders;

public sealed class OrderConstruction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Number { get; set; } = 1;

    public string Name { get; set; } = "Конструкция 1";

    public bool IsSelectedForCalculation { get; set; } = true;

    public ConstructionGeometry Geometry { get; set; } = new();

    public ConstructionCostBreakdown Cost { get; } = new();

    public ConstructionMetrics Metrics { get; set; } = new();

    public string ProfileSystemName { get; set; } = "";

    public string DimensionsDisplay =>
        Geometry.OuterWidthMm > 0 && Geometry.OuterHeightMm > 0
            ? $"{Geometry.OuterWidthMm:0} × {Geometry.OuterHeightMm:0}"
            : "—";

    public string AreaDisplay => Metrics.ProductAreaSqM > 0
        ? $"{Metrics.ProductAreaSqM:N2} м²"
        : "—";

    public string QuantityDisplay => $"{Math.Max(1, Cost.Quantity)} шт.";

    public string StatusDisplay => Geometry.OuterWidthMm > 0 && Geometry.OuterHeightMm > 0
        ? "✔ Рассчитано"
        : "Ожидает расчёта";

    public string PerimeterDisplay
    {
        get
        {
            if (Geometry.OuterWidthMm <= 0 || Geometry.OuterHeightMm <= 0)
            {
                return "—";
            }

            var perimeterM = (Geometry.OuterWidthMm + Geometry.OuterHeightMm) * 2m / 1000m;
            return $"{perimeterM:N2} м";
        }
    }

    public string ProfileDisplay => string.IsNullOrWhiteSpace(ProfileSystemName)
        ? "Пока пусто"
        : ProfileSystemName;

    public string ReinforcementDisplay => "Пока пусто";

    public string GlazingDisplay =>
        Geometry.Elements.FirstOrDefault(e => e.Kind == GeometryElementKind.Glazing)?.Label
        ?? "Пока пусто";

    public string HardwareDisplay =>
        Geometry.Elements.FirstOrDefault(e => e.Kind == GeometryElementKind.Hardware)?.Label
        ?? "Пока пусто";
}

public sealed class ConstructionMetrics
{
    public decimal GlazingAreaSqM { get; set; }

    public decimal ProductAreaSqM { get; set; }

    public decimal WeightKg { get; set; }

    public TimeSpan ManufacturingTime { get; set; }
}
