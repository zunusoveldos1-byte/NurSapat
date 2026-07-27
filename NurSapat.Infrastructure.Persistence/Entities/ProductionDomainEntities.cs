namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class PriceListVersionEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public ICollection<PriceListItemEntity> Items { get; set; } = [];
    public ICollection<OrderCalculationSnapshotEntity> CalculationSnapshots { get; set; } = [];
}

public sealed class PriceListItemEntity
{
    public Guid Id { get; set; }
    public Guid PriceListVersionId { get; set; }
    public string MaterialArticle { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal BaseRetailPrice { get; set; }
    public decimal DealerPrice { get; set; }
    public PriceListVersionEntity PriceListVersion { get; set; } = null!;
}

public sealed class OrderCalculationSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid PriceListVersionId { get; set; }
    public string CurrencyCode { get; set; } = "KGS";
    public decimal ExchangeRate { get; set; }
    public DateTime SnapshotDate { get; set; }
    public string ItemsSnapshotJson { get; set; } = "[]";
    public decimal TotalCostPrice { get; set; }
    public decimal TotalMarginPercent { get; set; }
    public decimal TotalDiscountPercent { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderEntity Order { get; set; } = null!;
    public PriceListVersionEntity PriceListVersion { get; set; } = null!;
}

public sealed class TechnologyRuleEntity
{
    public Guid Id { get; set; }
    public string ProfileSystemCode { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public decimal MinRadiusMm { get; set; }
    public decimal MaxSashWidthMm { get; set; }
    public decimal MaxSashHeightMm { get; set; }
    public decimal MaxSashWeightKg { get; set; }
    public decimal MaxInertiaIx { get; set; }
}

public sealed class ProductionHardwareKitEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemBrand { get; set; } = string.Empty;
    public decimal MinSashWidth { get; set; }
    public decimal MaxSashWidth { get; set; }
    public decimal MinSashHeight { get; set; }
    public decimal MaxSashHeight { get; set; }
    public decimal MaxWeightKg { get; set; }
    public string ComponentsJson { get; set; } = "[]";
}

public sealed class OperationRouteEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public decimal LaborCostPerHour { get; set; }
    public decimal StandardMinutesPerUnit { get; set; }
}

public sealed class ProductionBatchEntity
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PlannedStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public int Status { get; set; }
    public ICollection<StockPostingEntity> StockPostings { get; set; } = [];
    public ICollection<CuttingJobEntity> CuttingJobs { get; set; } = [];
}

public sealed class MaterialReservationEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string MaterialArticle { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsReleased { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderEntity Order { get; set; } = null!;
}

public sealed class StockPostingEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? BatchId { get; set; }
    public string MaterialArticle { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int OperationType { get; set; }
    public DateTime PostedAt { get; set; }
    public OrderEntity Order { get; set; } = null!;
    public ProductionBatchEntity? Batch { get; set; }
}

public sealed class CuttingJobEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public int MaterialType { get; set; }
    public string CuttingDataJson { get; set; } = "{}";
    public bool IsExecuted { get; set; }
    public ProductionBatchEntity Batch { get; set; } = null!;
}
