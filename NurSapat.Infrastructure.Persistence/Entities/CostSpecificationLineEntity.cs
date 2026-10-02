namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class CostSpecificationLineEntity
{
    public Guid Id { get; set; }

    public Guid OrderConstructionId { get; set; }

    public Guid MaterialId { get; set; }

    public Guid? ConstructionMaterialId { get; set; }

    public int Category { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public string Article { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal WastePercent { get; set; }

    public OrderConstructionEntity OrderConstruction { get; set; } = null!;
}
