using Microsoft.EntityFrameworkCore;
using NurSapat.Infrastructure.Persistence.Abstractions;
using NurSapat.Infrastructure.Persistence.Mapping;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class ErpMaterialRepository : IErpMaterialRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public ErpMaterialRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<ErpMaterialDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await QueryMaterials(dbContext)
            .FirstOrDefaultAsync(material => material.Id == id, cancellationToken);

        return entity is null ? null : ErpMaterialMapper.ToModel(entity);
    }

    public async Task<ErpMaterialDetail?> GetByArticleAsync(string article, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await QueryMaterials(dbContext)
            .FirstOrDefaultAsync(material => material.Article == article, cancellationToken);

        return entity is null ? null : ErpMaterialMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<ErpMaterialDetail>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await QueryMaterials(dbContext)
            .OrderBy(material => material.Article)
            .ToListAsync(cancellationToken);

        return entities.Select(ErpMaterialMapper.ToModel).ToList();
    }

    public async Task SaveAsync(ErpMaterialDetail material, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = ErpMaterialMapper.ToEntity(material);
        AttachMaterialChildren(entity);
        dbContext.Materials.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ErpMaterialDetail material, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.Materials
            .Include(m => m.ColorPrices)
            .Include(m => m.DimensionPrices)
            .FirstOrDefaultAsync(m => m.Id == material.Id, cancellationToken);

        if (existing is null)
        {
            await SaveAsync(material, cancellationToken);
            return;
        }

        dbContext.MaterialColorPrices.RemoveRange(existing.ColorPrices);
        dbContext.MaterialDimensionPrices.RemoveRange(existing.DimensionPrices);

        var replacement = ErpMaterialMapper.ToEntity(material);
        AttachMaterialChildren(replacement);
        dbContext.Entry(existing).CurrentValues.SetValues(replacement);
        existing.ColorPrices = replacement.ColorPrices;
        existing.DimensionPrices = replacement.DimensionPrices;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Materials.FirstOrDefaultAsync(material => material.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.Materials.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConstructionElementModel>> GetConstructionElementsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.ConstructionElements
            .AsNoTracking()
            .Include(element => element.AssociatedSeries)
            .Include(element => element.HardwareRules)
            .OrderBy(element => element.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ErpMaterialMapper.ToModel).ToList();
    }

    public async Task SaveConstructionElementAsync(
        ConstructionElementModel element,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = ErpMaterialMapper.ToEntity(element);
        entity.AssociatedSeries = element.AssociatedSeriesIds
            .Select(seriesId => new Entities.ConstructionElementSeriesEntity
            {
                ConstructionElementId = element.Id,
                SeriesId = seriesId
            })
            .ToList();

        foreach (var rule in entity.HardwareRules)
        {
            rule.ConstructionElementId = element.Id;
        }

        dbContext.ConstructionElements.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateConstructionElementAsync(
        ConstructionElementModel element,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.ConstructionElements
            .Include(e => e.AssociatedSeries)
            .Include(e => e.HardwareRules)
            .FirstOrDefaultAsync(e => e.Id == element.Id, cancellationToken);

        if (existing is null)
        {
            await SaveConstructionElementAsync(element, cancellationToken);
            return;
        }

        dbContext.ConstructionElementSeries.RemoveRange(existing.AssociatedSeries);
        dbContext.HardwareAutoPickRules.RemoveRange(existing.HardwareRules);

        var replacement = ErpMaterialMapper.ToEntity(element);
        replacement.AssociatedSeries = element.AssociatedSeriesIds
            .Select(seriesId => new Entities.ConstructionElementSeriesEntity
            {
                ConstructionElementId = element.Id,
                SeriesId = seriesId
            })
            .ToList();
        replacement.HardwareRules = element.HardwareRules.Select(rule => new Entities.HardwareAutoPickRuleEntity
        {
            Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id,
            ConstructionElementId = element.Id,
            MinWidthFalzMm = rule.MinWidthFalzMm,
            MaxWidthFalzMm = rule.MaxWidthFalzMm,
            MinHeightFalzMm = rule.MinHeightFalzMm,
            MaxHeightFalzMm = rule.MaxHeightFalzMm,
            ScissorsCount = rule.ScissorsCount,
            MiddleLockCount = rule.MiddleLockCount
        }).ToList();

        dbContext.Entry(existing).CurrentValues.SetValues(replacement);
        existing.AssociatedSeries = replacement.AssociatedSeries;
        existing.HardwareRules = replacement.HardwareRules;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteConstructionElementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.ConstructionElements.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.ConstructionElements.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Entities.MaterialEntity> QueryMaterials(NurSapatDbContext dbContext) =>
        dbContext.Materials
            .AsNoTracking()
            .Include(material => material.ColorPrices)
            .Include(material => material.DimensionPrices);

    private static void AttachMaterialChildren(Entities.MaterialEntity entity)
    {
        foreach (var colorPrice in entity.ColorPrices)
        {
            colorPrice.MaterialId = entity.Id;
        }

        foreach (var dimensionPrice in entity.DimensionPrices)
        {
            dimensionPrice.MaterialId = entity.Id;
        }
    }
}
