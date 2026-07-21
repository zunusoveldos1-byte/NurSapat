using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace NurSapat.Infrastructure.Sync.Remote;

/// <summary>PostgREST model for Supabase public.orders (without local SyncStatus).</summary>
[Table("orders")]
public sealed class RemoteOrderRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("profitability_percent")]
    public decimal ProfitabilityPercent { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("status")]
    public int Status { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [Column("dealer_id")]
    public Guid? DealerId { get; set; }

    [Column("current_price_mode")]
    public int CurrentPriceMode { get; set; }

    [Column("total_discount_percent")]
    public decimal TotalDiscountPercent { get; set; }

    [Column("total_discount_sum")]
    public decimal TotalDiscountSum { get; set; }

    [Column("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [Column("customer_phone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Column("site_address")]
    public string SiteAddress { get; set; } = string.Empty;

    [Column("dealer_name")]
    public string DealerName { get; set; } = string.Empty;

    [Column("order_manager_name")]
    public string OrderManagerName { get; set; } = string.Empty;

    [Column("customer_city")]
    public string CustomerCity { get; set; } = string.Empty;

    [Column("customer_code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Column("contract_number")]
    public string ContractNumber { get; set; } = string.Empty;

    [Column("contract_date")]
    public DateTime? ContractDate { get; set; }

    [Column("production_date")]
    public DateTime? ProductionDate { get; set; }

    [Column("delivery_date")]
    public DateTime? DeliveryDate { get; set; }

    [Column("installation_date")]
    public DateTime? InstallationDate { get; set; }

    [Column("has_delivery")]
    public bool HasDelivery { get; set; }

    [Column("has_installation")]
    public bool HasInstallation { get; set; }

    [Column("profile_note")]
    public string ProfileNote { get; set; } = string.Empty;

    [Column("production_note")]
    public string ProductionNote { get; set; } = string.Empty;

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "KGS";

    [Column("constructions_subtotal")]
    public decimal ConstructionsSubtotal { get; set; }

    [Column("discount2_percent")]
    public decimal Discount2Percent { get; set; }

    [Column("delivery_cost")]
    public decimal DeliveryCost { get; set; }

    [Column("additional_materials_cost")]
    public decimal AdditionalMaterialsCost { get; set; }

    [Column("additional_materials_installation_cost")]
    public decimal AdditionalMaterialsInstallationCost { get; set; }

    [Column("vat_percent")]
    public decimal VatPercent { get; set; }

    [Column("vat_mode")]
    public int VatMode { get; set; }

    [Column("agreed_amount")]
    public decimal AgreedAmount { get; set; }

    [Column("paid_amount")]
    public decimal PaidAmount { get; set; }

    [Column("payment_currency_code")]
    public string PaymentCurrencyCode { get; set; } = "KGS";

    [Column("selected_construction_id")]
    public Guid? SelectedConstructionId { get; set; }

    [Column("export_file_path")]
    public string? ExportFilePath { get; set; }

    [Column("constructions_json")]
    public string ConstructionsJson { get; set; } = "[]";

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
