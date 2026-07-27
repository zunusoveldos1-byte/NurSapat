using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Pricing;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class OrderCalculationSnapshotRepository : IOrderCalculationSnapshotRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public OrderCalculationSnapshotRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<OrderCalculationSnapshot?> GetLatestByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.OrderCalculationSnapshots.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(
        OrderCalculationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.OrderCalculationSnapshots.Add(ToEntity(snapshot));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static OrderCalculationSnapshot ToDomain(OrderCalculationSnapshotEntity x) => new()
    {
        Id = x.Id,
        OrderId = x.OrderId,
        PriceListVersionId = x.PriceListVersionId,
        CurrencyCode = x.CurrencyCode,
        ExchangeRate = x.ExchangeRate,
        SnapshotDate = DateTime.SpecifyKind(x.SnapshotDate, DateTimeKind.Utc),
        ItemsSnapshotJson = x.ItemsSnapshotJson,
        TotalCostPrice = x.TotalCostPrice,
        TotalMarginPercent = x.TotalMarginPercent,
        TotalDiscountPercent = x.TotalDiscountPercent,
        TotalPrice = x.TotalPrice
    };

    private static OrderCalculationSnapshotEntity ToEntity(OrderCalculationSnapshot x) => new()
    {
        Id = x.Id,
        OrderId = x.OrderId,
        PriceListVersionId = x.PriceListVersionId,
        CurrencyCode = x.CurrencyCode,
        ExchangeRate = x.ExchangeRate,
        SnapshotDate = x.SnapshotDate.ToUniversalTime(),
        ItemsSnapshotJson = x.ItemsSnapshotJson,
        TotalCostPrice = x.TotalCostPrice,
        TotalMarginPercent = x.TotalMarginPercent,
        TotalDiscountPercent = x.TotalDiscountPercent,
        TotalPrice = x.TotalPrice
    };
}
