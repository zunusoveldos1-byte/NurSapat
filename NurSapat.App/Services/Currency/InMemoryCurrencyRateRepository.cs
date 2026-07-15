using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Currency;

namespace NurSapat.App.Services.Currency;

/// <summary>Локальное хранилище курсов для режима Demo (без PostgreSQL).</summary>
public sealed class InMemoryCurrencyRateRepository : ICurrencyRateRepository
{
    private readonly List<CurrencyRate> _rates = CreateSeedRates();
    private readonly object _sync = new();

    public Task<bool> HasRatesForDateAsync(DateTime rateDate, CancellationToken cancellationToken = default)
    {
        var date = rateDate.Date;
        lock (_sync)
        {
            return Task.FromResult(_rates.Any(rate => rate.RateDate.Date == date));
        }
    }

    public Task UpsertRatesAsync(IReadOnlyList<CurrencyRate> rates, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            foreach (var rate in rates)
            {
                var index = _rates.FindIndex(
                    item => item.CurrencyCode == rate.CurrencyCode && item.RateDate.Date == rate.RateDate.Date);

                if (index >= 0)
                {
                    _rates[index].Rate = rate.Rate;
                    _rates[index].CreatedAt = rate.CreatedAt;
                }
                else
                {
                    _rates.Add(rate);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<CurrencyRate?> FindNearestRateAsync(
        string currencyCode,
        DateTime targetDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(currencyCode);
        if (IsKgs(normalizedCode))
        {
            return Task.FromResult<CurrencyRate?>(new CurrencyRate
            {
                CurrencyCode = "KGS",
                Rate = 1m,
                RateDate = targetDate.Date
            });
        }

        var date = targetDate.Date;
        lock (_sync)
        {
            var nearest = _rates
                .Where(rate => rate.CurrencyCode == normalizedCode && rate.RateDate.Date <= date)
                .OrderByDescending(rate => rate.RateDate)
                .FirstOrDefault();

            return Task.FromResult(nearest);
        }
    }

    public Task<DateTime?> GetLatestRateDateAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var latest = _rates
                .OrderByDescending(rate => rate.RateDate)
                .Select(rate => (DateTime?)rate.RateDate.Date)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }
    }

    public Task<IReadOnlyList<CurrencyRate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<CurrencyRate> copy = _rates
                .OrderByDescending(rate => rate.RateDate)
                .ThenBy(rate => rate.CurrencyCode)
                .Select(rate => new CurrencyRate
                {
                    Id = rate.Id,
                    CurrencyCode = rate.CurrencyCode,
                    Rate = rate.Rate,
                    RateDate = rate.RateDate,
                    CreatedAt = rate.CreatedAt
                })
                .ToList();

            return Task.FromResult(copy);
        }
    }

    public Task DeleteRatesForDateAsync(DateTime rateDate, CancellationToken cancellationToken = default)
    {
        var date = rateDate.Date;
        lock (_sync)
        {
            _rates.RemoveAll(rate => rate.RateDate.Date == date);
        }

        return Task.CompletedTask;
    }

    private static List<CurrencyRate> CreateSeedRates()
    {
        var createdAt = DateTime.UtcNow;
        return
        [
            Create("USD", 86.20m, new DateTime(2026, 1, 1), createdAt),
            Create("EUR", 98.10m, new DateTime(2026, 1, 1), createdAt),
            Create("CNY", 12.55m, new DateTime(2026, 1, 1), createdAt),
            Create("UE", 86.20m, new DateTime(2026, 1, 1), createdAt),
            Create("RUB", 0.95m, new DateTime(2026, 1, 1), createdAt),
            Create("USD", 87.45m, new DateTime(2026, 7, 1), createdAt),
            Create("EUR", 99.62m, new DateTime(2026, 7, 1), createdAt),
            Create("CNY", 12.88m, new DateTime(2026, 7, 1), createdAt),
            Create("UE", 87.45m, new DateTime(2026, 7, 1), createdAt),
            Create("RUB", 0.98m, new DateTime(2026, 7, 1), createdAt)
        ];
    }

    private static CurrencyRate Create(string code, decimal rate, DateTime rateDate, DateTime createdAt) =>
        new()
        {
            CurrencyCode = code,
            Rate = rate,
            RateDate = rateDate,
            CreatedAt = createdAt
        };

    private static string NormalizeCode(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? "KGS"
            : currencyCode.Trim().ToUpperInvariant();

    private static bool IsKgs(string currencyCode) =>
        currencyCode is "KGS" or "SOM" or "СОМ";
}
