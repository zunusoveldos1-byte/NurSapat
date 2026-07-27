using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Infrastructure.Persistence.Seeding;

namespace NurSapat.App.Services.Persistence;

public interface IApplicationPersistenceInitializer
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);
}

public sealed class ApplicationPersistenceInitializer(
    IConfiguration configuration,
    IServiceProvider serviceProvider) : IApplicationPersistenceInitializer
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isReady;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_isReady || !configuration.IsPostgreSqlPersistence())
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isReady)
            {
                return;
            }

            var seeder = serviceProvider.GetRequiredService<InitialDataSeeder>();
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            _isReady = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
