using NurSapat.Core.Interfaces;
using NurSapat.Core.Interfaces.Currency;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

/// <summary>
/// Обёртка каталога цен: переводит цены материалов в KGS по историческому курсу на дату заказа.
/// </summary>
public sealed class HistoricalCurrencyMaterialPriceCatalog : IMaterialPriceCatalog
{
    private readonly IMaterialPriceCatalog _innerCatalog;
    private readonly ICurrencyService _currencyService;
    private readonly DateTime _orderDate;

    public HistoricalCurrencyMaterialPriceCatalog(
        IMaterialPriceCatalog innerCatalog,
        ICurrencyService currencyService,
        DateTime orderDate)
    {
        _innerCatalog = innerCatalog;
        _currencyService = currencyService;
        _orderDate = orderDate.Date;
    }

    public MaterialPriceQuote? TryGetByMaterialId(Guid materialId) =>
        ConvertQuote(_innerCatalog.TryGetByMaterialId(materialId));

    public MaterialPriceQuote? TryGetForElement(GeometryElement element, GeometryElementKind effectiveKind) =>
        ConvertQuote(_innerCatalog.TryGetForElement(element, effectiveKind));

    public MaterialPriceQuote? TryGetByCategory(CostMaterialCategory category, MaterialPriceUnit unit) =>
        ConvertQuote(_innerCatalog.TryGetByCategory(category, unit));

    private MaterialPriceQuote? ConvertQuote(MaterialPriceQuote? quote)
    {
        if (quote is null)
        {
            return null;
        }

        if (IsKgs(quote.CurrencyCode))
        {
            return quote;
        }

        var rate = _currencyService.GetRateForDateAsync(quote.CurrencyCode, _orderDate).GetAwaiter().GetResult();
        return quote with
        {
            UnitPrice = quote.UnitPrice * rate,
            CurrencyCode = "KGS"
        };
    }

    private static bool IsKgs(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
        || currencyCode.Equals("KGS", StringComparison.OrdinalIgnoreCase)
        || currencyCode.Equals("SOM", StringComparison.OrdinalIgnoreCase);
}
