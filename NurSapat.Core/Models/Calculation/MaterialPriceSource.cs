namespace NurSapat.Core.Models.Calculation;

public sealed record MaterialPriceSource(
    string Name,
    decimal ListedUnitPrice,
    string CurrencyCode,
    decimal? ExchangeRateToKgs = null,
    DateTime? ExchangeRateDate = null);
