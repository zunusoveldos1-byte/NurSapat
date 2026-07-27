using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Orders;
using NurSapat.Infrastructure.Persistence.Mapping;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class SupplierRepository : ISupplierRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public SupplierRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);

        return entity is null ? null : SupplierMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(SupplierMapper.ToDomain).ToList();
    }

    public async Task SaveAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Suppliers.Add(SupplierMapper.ToEntity(supplier));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.Suppliers.Add(SupplierMapper.ToEntity(supplier));
        }
        else
        {
            dbContext.Entry(entity).CurrentValues.SetValues(SupplierMapper.ToEntity(supplier));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.Suppliers.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
