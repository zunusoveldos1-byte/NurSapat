namespace NurSapat.Core.Interfaces.Currency;

public sealed record ImportedCurrencyRate(string CurrencyCode, decimal Rate);

public sealed record CurrencyRateImportResult(DateTime RateDate, IReadOnlyList<ImportedCurrencyRate> Rates);

public interface ICurrencyRateImporter
{
    Task<CurrencyRateImportResult> ImportDailyRatesAsync(CancellationToken cancellationToken = default);
}
