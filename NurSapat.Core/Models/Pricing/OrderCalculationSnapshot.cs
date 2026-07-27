namespace NurSapat.Core.Models.Pricing;

public sealed class OrderCalculationSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid PriceListVersionId { get; set; }
    public string CurrencyCode { get; set; } = "KGS";
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public string ItemsSnapshotJson { get; set; } = "[]";
    public decimal TotalCostPrice { get; set; }
    public decimal TotalMarginPercent { get; set; }
    public decimal TotalDiscountPercent { get; set; }
    public decimal TotalPrice { get; set; }
}
