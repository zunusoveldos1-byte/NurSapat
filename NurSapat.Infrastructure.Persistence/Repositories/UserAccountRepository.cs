using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Users;
using NurSapat.Infrastructure.Persistence.Mapping;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public UserAccountRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

        return entity is null ? null : UserAccountMapper.ToDomain(entity);
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        var entity = entities.FirstOrDefault(user =>
            user.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        return entity is null ? null : UserAccountMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);

        return entities.Select(UserAccountMapper.ToDomain).ToList();
    }

    public async Task SaveAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Users.Add(UserAccountMapper.ToEntity(user));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == user.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.Users.Add(UserAccountMapper.ToEntity(user));
        }
        else
        {
            dbContext.Entry(entity).CurrentValues.SetValues(UserAccountMapper.ToEntity(user));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.Users.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
