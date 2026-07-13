using NurSapat.Core.Interfaces.Currency;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Currency;

namespace NurSapat.Core.Services.Currency;

public sealed class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRateRepository _repository;
    private readonly ICurrencyRateImporter _importer;
    private readonly ICurrencySyncLogger _logger;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public CurrencyService(
        ICurrencyRateRepository repository,
        ICurrencyRateImporter importer,
        ICurrencySyncLogger logger)
    {
        _repository = repository;
        _importer = importer;
        _logger = logger;
    }

    public async Task<CurrencySyncResult> CheckAndSyncRatesAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var today = DateTime.Today;
            if (await _repository.HasRatesForDateAsync(today, cancellationToken))
            {
                return new CurrencySyncResult(CurrencySyncStatus.UpToDate, today, today);
            }

            try
            {
                var importResult = await _importer.ImportDailyRatesAsync(cancellationToken);
                var downloadedAt = DateTime.UtcNow;
                var entities = importResult.Rates
                    .Select(rate => new CurrencyRate
                    {
                        CurrencyCode = rate.CurrencyCode,
                        Rate = rate.Rate,
                        RateDate = importResult.RateDate.Date,
                        CreatedAt = downloadedAt
                    })
                    .ToList();

                await _repository.UpsertRatesAsync(entities, cancellationToken);
                return new CurrencySyncResult(
                    CurrencySyncStatus.Imported,
                    importResult.RateDate.Date,
                    importResult.RateDate.Date);
            }
            catch (Exception ex) when (IsNetworkOrImportFailure(ex))
            {
                var latestDate = await _repository.GetLatestRateDateAsync(cancellationToken);
                var fallbackMessage = latestDate is null
                    ? "Не удалось загрузить курсы НБКР: нет сети и локальная история курсов пуста."
                    : $"Не удалось загрузить курсы НБКР: нет сети. Используются курсы от {latestDate:dd.MM.yyyy}.";

                _logger.LogWarning(fallbackMessage);
                return new CurrencySyncResult(CurrencySyncStatus.Offline, null, latestDate);
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<decimal> GetRateForDateAsync(
        string currencyCode,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(currencyCode);
        if (IsKgs(normalizedCode))
        {
            return 1m;
        }

        var rate = await _repository.FindNearestRateAsync(normalizedCode, date, cancellationToken);
        if (rate is null)
        {
            throw new InvalidOperationException(
                $"Курс {normalizedCode} на дату {date:dd.MM.yyyy} (или ближайший предыдущий) не найден.");
        }

        return rate.Rate;
    }

    public async Task<CurrencyRateRecord?> FindRateRecordAsync(
        string currencyCode,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(currencyCode);
        if (IsKgs(normalizedCode))
        {
            return new CurrencyRateRecord("KGS", 1m, date.Date, DateTime.UtcNow);
        }

        var rate = await _repository.FindNearestRateAsync(normalizedCode, date, cancellationToken);
        return rate is null
            ? null
            : new CurrencyRateRecord(rate.CurrencyCode, rate.Rate, rate.RateDate, rate.CreatedAt);
    }

    public decimal ConvertToKgs(decimal amount, string currencyCode, DateTime orderDate) =>
        ConvertToKgsAsync(amount, currencyCode, orderDate).GetAwaiter().GetResult();

    public async Task<decimal> ConvertToKgsAsync(
        decimal amount,
        string currencyCode,
        DateTime orderDate,
        CancellationToken cancellationToken = default)
    {
        if (amount == 0)
        {
            return 0m;
        }

        var rate = await GetRateForDateAsync(currencyCode, orderDate, cancellationToken);
        return amount * rate;
    }

    private static string NormalizeCode(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? "KGS"
            : currencyCode.Trim().ToUpperInvariant();

    private static bool IsKgs(string currencyCode) =>
        currencyCode is "KGS" or "SOM" or "СОМ";

    private static bool IsNetworkOrImportFailure(Exception ex) =>
        ex is HttpRequestException
            or TaskCanceledException
            or TimeoutException
            or InvalidOperationException;
}
