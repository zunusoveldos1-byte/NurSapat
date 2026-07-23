using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Help;
using NurSapat.App.Services.Persistence;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Core.Interfaces.Currency;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Infrastructure.Persistence.Seeding;
using NurSapat.Infrastructure.Sync.Abstractions;
using System.Diagnostics;

namespace NurSapat.App;

public partial class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public static IServiceProvider? ServiceProvider { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ServiceProvider = serviceProvider;
    }

    public App()
    {
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Avalonia.Controls.Design.IsDesignMode)
            {
                desktop.MainWindow = new MainWindow();
            }
            else
            {
                // Migrate/seed MUST complete before MainWindow resolves SettingsService /
                // PG stores — otherwise sync EF calls deadlock on the Avalonia UI thread.
                EnsurePersistenceReady();
                desktop.MainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
                _ = SyncCurrencyRatesOnStartupAsync();
                _ = InitializeOfflineSyncAsync();
                var updateCoordinator = _serviceProvider.GetRequiredService<UpdateStartupCoordinator>();
                updateCoordinator.Initialize(desktop.Args ?? Array.Empty<string>());

                desktop.Exit += (_, _) =>
                {
                    try
                    {
                        var sync = _serviceProvider?.GetService<IBackgroundSynchronizer>();
                        sync?.StopAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Stop sync: {ex.Message}");
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeOfflineSyncAsync()
    {
        try
        {
            var materials = _serviceProvider!.GetRequiredService<IOfflineMaterialRepository>();
            var orders = _serviceProvider.GetRequiredService<IOfflineOrderRepository>();
            var cachedUsers = _serviceProvider.GetRequiredService<ICachedUserRepository>();
            var releases = _serviceProvider.GetRequiredService<IGitHubReleaseCache>();
            await materials.EnsureSchemaAsync().ConfigureAwait(false);
            await orders.EnsureSchemaAsync().ConfigureAwait(false);
            await cachedUsers.EnsureSchemaAsync().ConfigureAwait(false);
            await releases.EnsureSchemaAsync().ConfigureAwait(false);

            var auth = _serviceProvider.GetRequiredService<ISupabaseAuthService>();
            await auth.RestoreSessionAsync().ConfigureAwait(false);

            var synchronizer = _serviceProvider.GetRequiredService<IBackgroundSynchronizer>();
            synchronizer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Offline sync init: {ex}");
        }
    }

    private void EnsurePersistenceReady()
    {
        try
        {
            var configuration = _serviceProvider!.GetRequiredService<IConfiguration>();

            // Offline-First: Sync/Supabase Auth + SQLite — never open Postgres at startup.
            var syncEnabled = configuration.GetValue("Sync:Enabled", false);
            if (syncEnabled || !configuration.IsPostgreSqlPersistence())
            {
                return;
            }

            var seeder = _serviceProvider.GetRequiredService<InitialDataSeeder>();
            PersistenceSync.Run(() => seeder.SeedAsync());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Seed при старте: {ex}");
            Console.WriteLine($"!!! Не удалось инициализировать PostgreSQL: {ex}");
        }
    }

    private async Task SyncCurrencyRatesOnStartupAsync()
    {
        try
        {
            var currencyService = _serviceProvider!.GetRequiredService<ICurrencyService>();
            await currencyService.CheckAndSyncRatesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Синхронизация курсов НБКР при старте: {ex.Message}");
        }
    }
}
