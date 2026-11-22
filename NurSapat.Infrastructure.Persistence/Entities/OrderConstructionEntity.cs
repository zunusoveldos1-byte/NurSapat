namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class OrderConstructionEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public int Number { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsSelectedForCalculation { get; set; }

    public decimal OuterWidthMm { get; set; }

    public decimal OuterHeightMm { get; set; }

    public string DrawingJson { get; set; } = "{}";

    public decimal GlazingAreaSqM { get; set; }

    public decimal ProductAreaSqM { get; set; }

    public decimal WeightKg { get; set; }

    public long ManufacturingTimeTicks { get; set; }

    public decimal ProfileCost { get; set; }

    public decimal ComponentsCost { get; set; }

    public decimal SealantCost { get; set; }

    public decimal GlazingCost { get; set; }

    public decimal HardwareCost { get; set; }

    public decimal InstallationMaterialsCost { get; set; }

    public decimal Expenses { get; set; }

    public int ExpensesMode { get; set; }

    public decimal LaborCost { get; set; }

    public decimal MaterialsWithoutMarkup { get; set; }

    public decimal ProfitabilityPercent { get; set; }

    public decimal AdditionalExpensesPercent { get; set; }

    public decimal AdditionalExpensesFixed { get; set; }

    public decimal Discount1Percent { get; set; }

    public decimal DemolitionCost { get; set; }

    public decimal InstallationCost { get; set; }

    public int Quantity { get; set; }

    public string ProfileSystemName { get; set; } = string.Empty;

    public string ColorName { get; set; } = "Белый";

    public string ColorHex { get; set; } = "#FFFFFF";

    public bool ChangeProfile { get; set; }

    public bool ChangeImposts { get; set; }

    public bool ChangeSashes { get; set; }

    public bool ChangeLayout { get; set; }

    public bool ChangeDecorativeElements { get; set; }

    public DateTimeOffset? ManufactureDate { get; set; }

    public DateTimeOffset? DeliveryDate { get; set; }

    public string Manager { get; set; } = string.Empty;

    public string Supervisor { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public OrderEntity Order { get; set; } = null!;

    public ICollection<GeometryElementEntity> GeometryElements { get; set; } = [];

    public ICollection<CostSpecificationLineEntity> SpecificationLines { get; set; } = [];
}
