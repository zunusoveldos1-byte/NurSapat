namespace NurSapat.Core.Models.Orders;

public sealed class OrderTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid? ManufacturerId { get; set; }

    public double WindPressure { get; set; }

    public decimal ProfitMargin { get; set; }

    public double AdditionalExpensesPercent { get; set; }

    public decimal AdditionalExpensesFixed { get; set; }

    public decimal DismantlingPrice { get; set; }

    public decimal InstallationPrice { get; set; }

    public string CurrencyCode { get; set; } = "KGS";

    public string DefaultOrderNote { get; set; } = string.Empty;

    public int DefaultProductionDays { get; set; } = 14;

    public decimal DefaultPrepaymentPercent { get; set; } = 30m;

    public bool ApplyDealerDiscount { get; set; }

    public decimal DealerDiscountPercent { get; set; }
}
