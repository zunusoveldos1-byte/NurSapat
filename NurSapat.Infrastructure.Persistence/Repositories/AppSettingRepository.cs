using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Repositories;

public sealed class AppSettingRepository : IAppSettingRepository
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;

    public AppSettingRepository(IDbContextFactory<NurSapatDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);

        return entity?.Value;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.AppSettings.AsNoTracking().ToListAsync(cancellationToken);
        return entities.ToDictionary(setting => setting.Key, setting => setting.Value);
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.AppSettings.FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
        if (entity is null)
        {
            dbContext.AppSettings.Add(new AppSettingEntity
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            entity.Value = value;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.AppSettings.FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.AppSettings.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
