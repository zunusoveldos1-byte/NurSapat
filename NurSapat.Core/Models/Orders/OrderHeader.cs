namespace NurSapat.Core.Models.Orders;

public sealed class OrderHeader
{
    public string OrderNumber { get; set; } = "";

    public DateTime OrderDate { get; set; } = DateTime.Today;

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public Guid? CustomerId { get; set; }

    public Guid? DealerId { get; set; }

    public PriceModeType CurrentPriceMode { get; set; } = PriceModeType.Retail;

    public decimal TotalDiscountPercent { get; set; }

    public decimal TotalDiscountSum { get; set; }

    public string CustomerName { get; set; } = "";

    public string CustomerPhone { get; set; } = "";

    public string SiteAddress { get; set; } = "";

    public string DealerName { get; set; } = "";

    public string OrderManagerName { get; set; } = "";

    public string CustomerCity { get; set; } = "";

    public string CustomerCode { get; set; } = "";

    public string ContractNumber { get; set; } = "";

    public DateTime? ContractDate { get; set; }

    public DateTime? ProductionDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? InstallationDate { get; set; }

    public bool HasDelivery { get; set; }

    public bool HasInstallation { get; set; }

    public string ProfileNote { get; set; } = "";

    /// <summary>Заметки к производству.</summary>
    public string ProductionNote { get; set; } = "";

    public string Note { get; set; } = "";

    public string CurrencyCode { get; set; } = "KGS";
}
