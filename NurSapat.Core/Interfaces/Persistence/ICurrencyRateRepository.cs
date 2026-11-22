using NurSapat.Core.Models.Currency;

namespace NurSapat.Core.Interfaces.Persistence;

public interface ICurrencyRateRepository
{
    Task<bool> HasRatesForDateAsync(DateTime rateDate, CancellationToken cancellationToken = default);

    Task UpsertRatesAsync(IReadOnlyList<CurrencyRate> rates, CancellationToken cancellationToken = default);

    Task<CurrencyRate?> FindNearestRateAsync(
        string currencyCode,
        DateTime targetDate,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLatestRateDateAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrencyRate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task DeleteRatesForDateAsync(DateTime rateDate, CancellationToken cancellationToken = default);
}
