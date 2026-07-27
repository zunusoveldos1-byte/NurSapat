using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Pricing;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class PriceListRepository : IPriceListRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public PriceListRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<PriceListVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.PriceListVersions.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(PriceListVersion version, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.PriceListVersions.Add(ToEntity(version));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PriceListVersion version, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.PriceListVersions.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == version.Id, cancellationToken)
            ?? throw new InvalidOperationException("Price list version was not found.");
        existing.Title = version.Title;
        existing.EffectiveFrom = version.EffectiveFrom;
        existing.EffectiveTo = version.EffectiveTo;
        existing.IsActive = version.IsActive;

        var requestedIds = version.Items.Select(x => x.Id).ToHashSet();
        db.PriceListItems.RemoveRange(existing.Items.Where(x => !requestedIds.Contains(x.Id)));
        foreach (var item in version.Items)
        {
            var entity = existing.Items.FirstOrDefault(x => x.Id == item.Id);
            if (entity is null)
            {
                existing.Items.Add(new PriceListItemEntity
                {
                    Id = item.Id,
                    PriceListVersionId = version.Id,
                    MaterialArticle = item.MaterialArticle,
                    PurchasePrice = item.PurchasePrice,
                    BaseRetailPrice = item.BaseRetailPrice,
                    DealerPrice = item.DealerPrice
                });
                continue;
            }

            entity.MaterialArticle = item.MaterialArticle;
            entity.PurchasePrice = item.PurchasePrice;
            entity.BaseRetailPrice = item.BaseRetailPrice;
            entity.DealerPrice = item.DealerPrice;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static PriceListVersion ToDomain(PriceListVersionEntity x)
    {
        var domain = new PriceListVersion
        {
            Id = x.Id,
            Title = x.Title,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            IsActive = x.IsActive
        };
        domain.Items.AddRange(x.Items.Select(item => new PriceListItem
        {
            Id = item.Id,
            PriceListVersionId = item.PriceListVersionId,
            MaterialArticle = item.MaterialArticle,
            PurchasePrice = item.PurchasePrice,
            BaseRetailPrice = item.BaseRetailPrice,
            DealerPrice = item.DealerPrice
        }));
        return domain;
    }

    private static PriceListVersionEntity ToEntity(PriceListVersion x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        EffectiveFrom = x.EffectiveFrom,
        EffectiveTo = x.EffectiveTo,
        IsActive = x.IsActive,
        Items = x.Items.Select(item => new PriceListItemEntity
        {
            Id = item.Id,
            PriceListVersionId = x.Id,
            MaterialArticle = item.MaterialArticle,
            PurchasePrice = item.PurchasePrice,
            BaseRetailPrice = item.BaseRetailPrice,
            DealerPrice = item.DealerPrice
        }).ToList()
    };
}
