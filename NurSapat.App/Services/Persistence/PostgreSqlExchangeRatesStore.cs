using System.Security.Cryptography;
using System.Text;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Currency;

namespace NurSapat.App.Services.Persistence;

/// <summary>
/// Adapts exchange-rate packages used by the dictionary UI onto <see cref="ICurrencyRateRepository"/> rows
/// (USD/EUR/CNY/UE per package StartDate).
/// </summary>
public sealed class PostgreSqlExchangeRatesStore : IExchangeRatesStore
{
    private static readonly string[] PackageCodes = ["USD", "EUR", "CNY", "UE"];

    private readonly ICurrencyRateRepository _repository;

    public PostgreSqlExchangeRatesStore(ICurrencyRateRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<ExchangeRatePackage> GetAll() =>
        BuildPackages(PersistenceSync.Run(() => _repository.GetAllAsync()))
            .OrderByDescending(package => package.StartDate)
            .ToList();

    public ExchangeRatePackage? GetById(Guid id) =>
        GetAll().FirstOrDefault(package => package.Id == id);

    public ExchangeRatePackage? GetByStartDate(DateTime startDate)
    {
        var date = startDate.Date;
        return GetAll().FirstOrDefault(package => package.StartDate.Date == date);
    }

    public ExchangeRatePackage? GetRatesForDate(DateTime date)
    {
        var targetDate = date.Date;
        return GetAll()
            .Where(package => package.StartDate.Date <= targetDate)
            .OrderByDescending(package => package.StartDate)
            .FirstOrDefault();
    }

    public void Add(ExchangeRatePackage package)
    {
        package.Id = PackageIdForDate(package.StartDate);
        UpsertPackageRates(package);
    }

    public void Update(ExchangeRatePackage package)
    {
        var existing = GetById(package.Id);
        if (existing is not null && existing.StartDate.Date != package.StartDate.Date)
        {
            PersistenceSync.Run(() => _repository.DeleteRatesForDateAsync(existing.StartDate));
        }

        package.Id = PackageIdForDate(package.StartDate);
        UpsertPackageRates(package);
    }

    public void Delete(Guid id)
    {
        var existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        PersistenceSync.Run(() => _repository.DeleteRatesForDateAsync(existing.StartDate));
    }

    private void UpsertPackageRates(ExchangeRatePackage package)
    {
        var date = package.StartDate.Date;
        var createdAt = DateTime.UtcNow;
        var rates = new List<CurrencyRate>
        {
            CreateRate("USD", package.UsdRate, date, createdAt),
            CreateRate("EUR", package.EurRate, date, createdAt),
            CreateRate("CNY", package.CnyRate, date, createdAt),
            CreateRate("UE", package.UeRate, date, createdAt)
        };

        PersistenceSync.Run(() => _repository.UpsertRatesAsync(rates));
    }

    private static IEnumerable<ExchangeRatePackage> BuildPackages(IReadOnlyList<CurrencyRate> rates)
    {
        foreach (var group in rates
                     .Where(rate => PackageCodes.Contains(rate.CurrencyCode, StringComparer.OrdinalIgnoreCase))
                     .GroupBy(rate => rate.RateDate.Date))
        {
            var byCode = group.ToDictionary(
                rate => rate.CurrencyCode.ToUpperInvariant(),
                rate => rate.Rate,
                StringComparer.OrdinalIgnoreCase);

            yield return new ExchangeRatePackage
            {
                Id = PackageIdForDate(group.Key),
                StartDate = group.Key,
                UsdRate = byCode.GetValueOrDefault("USD"),
                EurRate = byCode.GetValueOrDefault("EUR"),
                CnyRate = byCode.GetValueOrDefault("CNY"),
                UeRate = byCode.GetValueOrDefault("UE", byCode.GetValueOrDefault("USD"))
            };
        }
    }

    private static CurrencyRate CreateRate(string code, decimal rate, DateTime date, DateTime createdAt) =>
        new()
        {
            CurrencyCode = code,
            Rate = rate,
            RateDate = date,
            CreatedAt = createdAt
        };

    internal static Guid PackageIdForDate(DateTime date)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"nursapat-fx-package:{date:yyyy-MM-dd}"));
        return new Guid(bytes);
    }
}
