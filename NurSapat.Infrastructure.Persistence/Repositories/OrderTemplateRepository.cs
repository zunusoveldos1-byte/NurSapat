using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Orders;
using NurSapat.Infrastructure.Persistence.Mapping;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class OrderTemplateRepository : IOrderTemplateRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public OrderTemplateRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<OrderTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.OrderTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(template => template.Id == id, cancellationToken);

        return entity is null ? null : OrderTemplateMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<OrderTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.OrderTemplates
            .AsNoTracking()
            .OrderBy(template => template.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(OrderTemplateMapper.ToDomain).ToList();
    }

    public async Task SaveAsync(OrderTemplate template, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.OrderTemplates.Add(OrderTemplateMapper.ToEntity(template));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OrderTemplate template, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.OrderTemplates.FirstOrDefaultAsync(item => item.Id == template.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.OrderTemplates.Add(OrderTemplateMapper.ToEntity(template));
        }
        else
        {
            dbContext.Entry(entity).CurrentValues.SetValues(OrderTemplateMapper.ToEntity(template));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.OrderTemplates.FirstOrDefaultAsync(template => template.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.OrderTemplates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
