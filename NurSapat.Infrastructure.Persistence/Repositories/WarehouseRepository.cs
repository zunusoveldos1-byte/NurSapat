using Microsoft.EntityFrameworkCore;
using NurSapat.Infrastructure.Persistence.Abstractions;
using NurSapat.Infrastructure.Persistence.Mapping;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public WarehouseRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<StockDocumentModel?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.StockDocuments
            .AsNoTracking()
            .Include(document => document.Lines)
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);

        return entity is null ? null : WarehouseMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<StockDocumentModel>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.StockDocuments
            .AsNoTracking()
            .Include(document => document.Lines)
            .OrderByDescending(document => document.Date)
            .ToListAsync(cancellationToken);

        return entities.Select(WarehouseMapper.ToModel).ToList();
    }

    public async Task SaveDocumentAsync(StockDocumentModel document, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = WarehouseMapper.ToEntity(document);
        foreach (var line in entity.Lines)
        {
            line.StockDocumentId = entity.Id;
        }

        dbContext.StockDocuments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDocumentAsync(StockDocumentModel document, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.StockDocuments
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == document.Id, cancellationToken);

        if (existing is null)
        {
            await SaveDocumentAsync(document, cancellationToken);
            return;
        }

        dbContext.StockDocumentLines.RemoveRange(existing.Lines);
        var replacement = WarehouseMapper.ToEntity(document);
        foreach (var line in replacement.Lines)
        {
            line.StockDocumentId = existing.Id;
        }

        dbContext.Entry(existing).CurrentValues.SetValues(replacement);
        existing.Lines = replacement.Lines;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.StockDocuments.FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.StockDocuments.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FinishedGoodModel?> GetFinishedGoodByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.FinishedGoods
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return entity is null ? null : WarehouseMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<FinishedGoodModel>> GetAllFinishedGoodsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.FinishedGoods
            .AsNoTracking()
            .OrderByDescending(item => item.DateIncoming)
            .ToListAsync(cancellationToken);

        return entities.Select(WarehouseMapper.ToModel).ToList();
    }

    public async Task SaveFinishedGoodAsync(FinishedGoodModel item, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.FinishedGoods.Add(WarehouseMapper.ToEntity(item));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFinishedGoodAsync(FinishedGoodModel item, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.FinishedGoods.FirstOrDefaultAsync(g => g.Id == item.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.FinishedGoods.Add(WarehouseMapper.ToEntity(item));
        }
        else
        {
            dbContext.Entry(entity).CurrentValues.SetValues(WarehouseMapper.ToEntity(item));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFinishedGoodAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.FinishedGoods.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.FinishedGoods.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
