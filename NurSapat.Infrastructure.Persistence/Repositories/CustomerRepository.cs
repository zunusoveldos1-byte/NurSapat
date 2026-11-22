using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Orders;
using NurSapat.Infrastructure.Persistence.Mapping;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public CustomerRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

        return entity is null ? null : CustomerMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.FullName)
            .ToListAsync(cancellationToken);

        return entities.Select(CustomerMapper.ToDomain).ToList();
    }

    public async Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Customers.Add(CustomerMapper.ToEntity(customer));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.Customers.Add(CustomerMapper.ToEntity(customer));
        }
        else
        {
            dbContext.Entry(entity).CurrentValues.SetValues(CustomerMapper.ToEntity(customer));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
