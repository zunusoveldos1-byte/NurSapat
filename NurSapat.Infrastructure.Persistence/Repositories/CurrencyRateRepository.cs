using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Currency;
using NurSapat.Infrastructure.Persistence.Mapping;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class CurrencyRateRepository : ICurrencyRateRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public CurrencyRateRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> HasRatesForDateAsync(DateTime rateDate, CancellationToken cancellationToken = default)
    {
        var date = rateDate.Date;
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.CurrencyRates
            .AsNoTracking()
            .AnyAsync(rate => rate.RateDate == date, cancellationToken);
    }

    public async Task UpsertRatesAsync(IReadOnlyList<CurrencyRate> rates, CancellationToken cancellationToken = default)
    {
        if (rates.Count == 0)
        {
            return;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        foreach (var rate in rates)
        {
            var entity = CurrencyRateMapper.ToEntity(rate);
            var existing = await dbContext.CurrencyRates
                .FirstOrDefaultAsync(
                    item => item.CurrencyCode == entity.CurrencyCode && item.RateDate == entity.RateDate,
                    cancellationToken);

            if (existing is null)
            {
                dbContext.CurrencyRates.Add(entity);
            }
            else
            {
                existing.Rate = entity.Rate;
                existing.CreatedAt = entity.CreatedAt;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrencyRate?> FindNearestRateAsync(
        string currencyCode,
        DateTime targetDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(currencyCode);
        if (IsKgs(normalizedCode))
        {
            return new CurrencyRate
            {
                CurrencyCode = "KGS",
                Rate = 1m,
                RateDate = targetDate.Date
            };
        }

        var date = targetDate.Date;
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await dbContext.CurrencyRates
            .AsNoTracking()
            .Where(rate => rate.CurrencyCode == normalizedCode && rate.RateDate <= date)
            .OrderByDescending(rate => rate.RateDate)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : CurrencyRateMapper.ToDomain(entity);
    }

    public async Task<DateTime?> GetLatestRateDateAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.CurrencyRates
            .AsNoTracking()
            .OrderByDescending(rate => rate.RateDate)
            .Select(rate => (DateTime?)rate.RateDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrencyRate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.CurrencyRates
            .AsNoTracking()
            .OrderByDescending(rate => rate.RateDate)
            .ThenBy(rate => rate.CurrencyCode)
            .ToListAsync(cancellationToken);

        return entities.Select(CurrencyRateMapper.ToDomain).ToList();
    }

    public async Task DeleteRatesForDateAsync(DateTime rateDate, CancellationToken cancellationToken = default)
    {
        var date = rateDate.Date;
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.CurrencyRates
            .Where(rate => rate.RateDate == date)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return;
        }

        dbContext.CurrencyRates.RemoveRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeCode(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? "KGS"
            : currencyCode.Trim().ToUpperInvariant();

    private static bool IsKgs(string currencyCode) =>
        currencyCode is "KGS" or "SOM" or "СОМ";
}
