namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class OrderEntity
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

    /// <summary>Optional path to the last .nsp export/backup; working order data lives in PostgreSQL.</summary>
    public string? ExportFilePath { get; set; }

    public CustomerEntity? Customer { get; set; }

    public ICollection<OrderConstructionEntity> Constructions { get; set; } = [];
}
