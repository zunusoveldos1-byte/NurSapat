using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Infrastructure.Persistence.Seeding;

namespace NurSapat.App.Services.Persistence;

public interface IApplicationPersistenceInitializer
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);

    Task PrepareRestoredDatabaseAsync(CancellationToken cancellationToken = default);
}

public sealed class ApplicationPersistenceInitializer(
    IConfiguration configuration,
    IServiceProvider serviceProvider) : IApplicationPersistenceInitializer
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isReady;

    public Task EnsureReadyAsync(CancellationToken cancellationToken = default) =>
        InitializeAsync(force: false, cancellationToken);

    public Task PrepareRestoredDatabaseAsync(CancellationToken cancellationToken = default) =>
        InitializeAsync(force: true, cancellationToken);

    private async Task InitializeAsync(bool force, CancellationToken cancellationToken)
    {
        if (!configuration.IsPostgreSqlPersistence())
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isReady && !force)
            {
                return;
            }

            var seeder = serviceProvider.GetRequiredService<InitialDataSeeder>();
            _isReady = false;
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            _isReady = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
