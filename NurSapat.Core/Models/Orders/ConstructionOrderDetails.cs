namespace NurSapat.Core.Models.Orders;

/// <summary>
/// Order fields edited in the construction workspace. A separate instance is
/// owned by every construction so switching cannot leak customer/date values.
/// </summary>
public sealed class ConstructionOrderDetails
{
    public bool IsInitialized { get; set; } = true;

    public DateTime OrderDate { get; set; } = DateTime.Today;

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public string SiteAddress { get; set; } = string.Empty;

    public string CustomerCity { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;

    public Guid? DealerId { get; set; }

    public string DealerName { get; set; } = string.Empty;

    public Guid? ManagerId { get; set; }

    public string OrderManagerName { get; set; } = string.Empty;

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

    public void CaptureFrom(OrderHeader header)
    {
        IsInitialized = true;
        OrderDate = header.OrderDate;
        CustomerId = header.CustomerId;
        CustomerName = header.CustomerName;
        CustomerPhone = header.CustomerPhone;
        SiteAddress = header.SiteAddress;
        CustomerCity = header.CustomerCity;
        CustomerCode = header.CustomerCode;
        DealerId = header.DealerId;
        DealerName = header.DealerName;
        ManagerId = header.ManagerId;
        OrderManagerName = header.OrderManagerName;
        ContractNumber = header.ContractNumber;
        ContractDate = header.ContractDate;
        ProductionDate = header.ProductionDate;
        DeliveryDate = header.DeliveryDate;
        InstallationDate = header.InstallationDate;
        HasDelivery = header.HasDelivery;
        HasInstallation = header.HasInstallation;
        ProfileNote = header.ProfileNote;
        ProductionNote = header.ProductionNote;
        Note = header.Note;
    }

    public void ApplyTo(OrderHeader header)
    {
        header.OrderDate = OrderDate;
        header.CustomerId = CustomerId;
        header.CustomerName = CustomerName;
        header.CustomerPhone = CustomerPhone;
        header.SiteAddress = SiteAddress;
        header.CustomerCity = CustomerCity;
        header.CustomerCode = CustomerCode;
        header.DealerId = DealerId;
        header.DealerName = DealerName;
        header.ManagerId = ManagerId;
        header.OrderManagerName = OrderManagerName;
        header.ContractNumber = ContractNumber;
        header.ContractDate = ContractDate;
        header.ProductionDate = ProductionDate;
        header.DeliveryDate = DeliveryDate;
        header.InstallationDate = InstallationDate;
        header.HasDelivery = HasDelivery;
        header.HasInstallation = HasInstallation;
        header.ProfileNote = ProfileNote;
        header.ProductionNote = ProductionNote;
        header.Note = Note;
    }

    public ConstructionOrderDetails Clone() => (ConstructionOrderDetails)MemberwiseClone();
}
