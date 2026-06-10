using NurSapat.Core.Models.Calculation;

namespace NurSapat.Core.Models.Orders;

public sealed class OrderConstruction
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public int Number { get; set; } = 1;

    public string Name { get; set; } = "Конструкция 1";

    public bool IsSelectedForCalculation { get; set; } = true;

    public ConstructionGeometry Geometry { get; set; } = new();

    public ConstructionCostBreakdown Cost { get; } = new();

    public ConstructionMetrics Metrics { get; set; } = new();

    public string ProfileSystemName { get; set; } = "";
}

public sealed class ConstructionMetrics
{
    public decimal GlazingAreaSqM { get; set; }

    public decimal ProductAreaSqM { get; set; }

    public decimal WeightKg { get; set; }

    public TimeSpan ManufacturingTime { get; set; }
}
