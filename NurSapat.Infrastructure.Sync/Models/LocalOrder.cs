namespace NurSapat.Infrastructure.Sync.Models;

/// <summary>
/// Local order row mirroring the cloud orders table plus sync metadata.
/// Field set matches NurSapat OrderEntity header/totals.
/// </summary>
public sealed class LocalOrder
{
    public Guid Id { get; set; }

    public decimal ProfitabilityPercent { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int Status { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? DealerId { get; set; }

    public int CurrentPriceMode { get; set; }
    public decimal TotalDiscountPercent { get; set; }
    public decimal TotalDiscountSum { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string SiteAddress { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public string OrderManagerName { get; set; } = string.Empty;
    public string CustomerCity { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;

    public DateTime? ContractDate { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? InstallationDate { get; set; }

    public bool HasDelivery { get; set; }
    public bool HasInstallation { get; set; }

    public string ProfileNote { get; set; } = string.Empty;
    public string ProductionNote { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "KGS";

    public decimal ConstructionsSubtotal { get; set; }
    public decimal Discount2Percent { get; set; }
    public decimal DeliveryCost { get; set; }
    public decimal AdditionalMaterialsCost { get; set; }
    public decimal AdditionalMaterialsInstallationCost { get; set; }
    public decimal VatPercent { get; set; }
    public int VatMode { get; set; }
    public decimal AgreedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentCurrencyCode { get; set; } = "KGS";

    public Guid? SelectedConstructionId { get; set; }
    public string? ExportFilePath { get; set; }

    /// <summary>JSON snapshot of constructions for offline round-trip.</summary>
    public string ConstructionsJson { get; set; } = "[]";

    public Guid? UserId { get; set; }
    public bool IsDeleted { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LastSyncError { get; set; }
}
