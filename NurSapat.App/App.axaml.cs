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
using NurSapat.Infrastructure.Sync.Abstractions;
using System.Diagnostics;

namespace NurSapat.App;

public partial class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public static IServiceProvider? ServiceProvider { get; private set; }

    private IServiceProvider Services =>
        _serviceProvider
        ?? throw new InvalidOperationException("Application services are not initialized.");

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
                var services = Services;
                desktop.MainWindow = services.GetRequiredService<MainWindow>();
                _ = InitializeOfflineSyncAsync();
                var updateCoordinator = services.GetRequiredService<UpdateStartupCoordinator>();
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
            var services = Services;
            var materials = services.GetRequiredService<IOfflineMaterialRepository>();
            var orders = services.GetRequiredService<IOfflineOrderRepository>();
            var cachedUsers = services.GetRequiredService<ICachedUserRepository>();
            var releases = services.GetRequiredService<IGitHubReleaseCache>();
            await materials.EnsureSchemaAsync().ConfigureAwait(false);
            await orders.EnsureSchemaAsync().ConfigureAwait(false);
            await cachedUsers.EnsureSchemaAsync().ConfigureAwait(false);
            await releases.EnsureSchemaAsync().ConfigureAwait(false);

            var auth = services.GetRequiredService<ISupabaseAuthService>();
            await auth.RestoreSessionAsync().ConfigureAwait(false);

            var synchronizer = services.GetRequiredService<IBackgroundSynchronizer>();
            synchronizer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Offline sync init: {ex}");
        }
    }

}
