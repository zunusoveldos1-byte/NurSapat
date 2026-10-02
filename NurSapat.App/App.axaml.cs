using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Persistence;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Core.Interfaces.Currency;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Infrastructure.Persistence.Seeding;
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
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void EnsurePersistenceReady()
    {
        try
        {
            var configuration = _serviceProvider!.GetRequiredService<IConfiguration>();
            if (!configuration.IsPostgreSqlPersistence())
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
            // Login/MainWindow still open; GetByUsername will surface a clear error if schema is broken.
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
