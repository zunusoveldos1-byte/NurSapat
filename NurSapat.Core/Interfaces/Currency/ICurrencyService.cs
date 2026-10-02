namespace NurSapat.Core.Interfaces.Currency;

public enum CurrencySyncStatus
{
    UpToDate,
    Imported,
    Offline
}

public sealed record CurrencySyncResult(
    CurrencySyncStatus Status,
    DateTime? RateDate,
    DateTime? LatestAvailableRateDate);

public interface ICurrencyService
{
    Task<CurrencySyncResult> CheckAndSyncRatesAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetRateForDateAsync(
        string currencyCode,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<CurrencyRateRecord?> FindRateRecordAsync(
        string currencyCode,
        DateTime date,
        CancellationToken cancellationToken = default);

    decimal ConvertToKgs(decimal amount, string currencyCode, DateTime orderDate);

    Task<decimal> ConvertToKgsAsync(
        decimal amount,
        string currencyCode,
        DateTime orderDate,
        CancellationToken cancellationToken = default);
}

public sealed record CurrencyRateRecord(
    string CurrencyCode,
    decimal Rate,
    DateTime RateDate,
    DateTime CreatedAt);
