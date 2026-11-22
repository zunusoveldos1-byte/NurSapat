using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models;
using NurSapat.Infrastructure.Persistence.Mapping;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private static readonly OrderLifecycleService Lifecycle = new();
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public OrderRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Constructions)
                .ThenInclude(construction => construction.GeometryElements)
            .Include(order => order.Constructions)
                .ThenInclude(construction => construction.SpecificationLines)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

        return entity is null ? null : OrderMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Constructions)
                .ThenInclude(construction => construction.GeometryElements)
            .Include(order => order.Constructions)
                .ThenInclude(construction => construction.SpecificationLines)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync(cancellationToken);

        return entities.Select(OrderMapper.ToDomain).ToList();
    }

    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = OrderMapper.ToEntity(order);
        AttachConstructionRelations(entity);
        dbContext.Orders.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.Orders
            .Include(o => o.Constructions)
                .ThenInclude(c => c.GeometryElements)
            .Include(o => o.Constructions)
                .ThenInclude(c => c.SpecificationLines)
            .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

        if (existing is null)
        {
            await SaveAsync(order, cancellationToken);
            return;
        }

        var currentStatus = (OrderStatus)existing.Status;
        var targetStatus = order.Header.Status;
        if (currentStatus != targetStatus)
        {
            var hasSnapshot = await dbContext.OrderCalculationSnapshots
                .AnyAsync(x => x.OrderId == order.Id, cancellationToken);
            var lifecycleOrder = OrderMapper.ToDomain(existing);
            Lifecycle.AssertCanTransition(lifecycleOrder, targetStatus, hasSnapshot);

            if (currentStatus >= OrderStatus.InProduction || targetStatus >= OrderStatus.InProduction)
            {
                existing.Status = (int)targetStatus;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
        }
        else
        {
            Lifecycle.AssertCanEdit(currentStatus);
        }

        var replacement = OrderMapper.ToEntity(order);
        AttachConstructionRelations(replacement);

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Children may use the same IDs (and construction numbers) in the replacement.
        // Complete their removal before EF tracks the replacement graph, inside one transaction.
        dbContext.OrderConstructions.RemoveRange(existing.Constructions);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Entry(existing).CurrentValues.SetValues(replacement);
        existing.SelectedConstructionId = replacement.SelectedConstructionId;
        existing.Constructions = replacement.Constructions;
        dbContext.OrderConstructions.AddRange(existing.Constructions);

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.Orders.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AttachConstructionRelations(Entities.OrderEntity entity)
    {
        foreach (var construction in entity.Constructions)
        {
            construction.OrderId = entity.Id;
            foreach (var element in construction.GeometryElements)
            {
                element.OrderConstructionId = construction.Id;
            }

            foreach (var line in construction.SpecificationLines)
            {
                line.OrderConstructionId = construction.Id;
            }
        }
    }
}
