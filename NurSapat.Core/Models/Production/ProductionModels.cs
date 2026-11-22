namespace NurSapat.Core.Models.Production;

public enum ProductionBatchStatus
{
    Planned = 0,
    InProgress = 10,
    Completed = 20,
    Cancelled = 99
}

public enum StockOperationType
{
    Receipt = 0,
    ConsumptionForProduction = 10,
    Return = 20
}

public enum CuttingMaterialType
{
    Profile = 0,
    Glass = 10,
    Sandwich = 20
}

public sealed class ProductionBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PlannedStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public ProductionBatchStatus Status { get; set; }
}

public sealed class MaterialReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string MaterialArticle { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsReleased { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class StockPosting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid? BatchId { get; set; }
    public string MaterialArticle { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public StockOperationType OperationType { get; set; }
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CuttingJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public CuttingMaterialType MaterialType { get; set; }
    public string CuttingDataJson { get; set; } = "{}";
    public bool IsExecuted { get; set; }
}
