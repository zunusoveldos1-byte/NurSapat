using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services;
using NurSapat.App.Services.Help;
using NurSapat.App.Services.Persistence;
using NurSapat.App.Services.Users;
using NurSapat.App.Models.Users;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Core.Interfaces.Currency;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Sync.Models;
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
                // Do not construct MainWindow/LoginView until the remembered
                // session has been evaluated. This prevents even a transient
                // login screen from appearing during offline auto-login.
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                _ = InitializeSessionAndStartupWindowAsync(desktop);
                var updateCoordinator = services.GetRequiredService<UpdateStartupCoordinator>();
                updateCoordinator.Initialize(desktop.Args ?? Array.Empty<string>());

                desktop.Exit += (_, _) =>
                {
                    try
                    {
                        var sync = _serviceProvider?.GetService<IBackgroundSynchronizer>();
                        sync?.StopAsync().GetAwaiter().GetResult();
                        var network = _serviceProvider?.GetService<INetworkStatusService>();
                        network?.StopAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        StartupTrace.Error("[LOG] Остановка фоновых сервисов при выходе", ex);
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeSessionAndStartupWindowAsync(
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            StartupTrace.Info("[LOG] Начало проверки сессии при старте...");
            var services = Services;
            var sessionCache = services.GetRequiredService<ProtectedSessionCache>();
            StartupTrace.Info(
                $"[LOG] Проверка файла session.dat: " +
                $"{(File.Exists(sessionCache.FilePath) ? "Существует" : "Не существует")} " +
                $"({sessionCache.FilePath})");

            var network = services.GetRequiredService<INetworkStatusService>();
            network.Start();

            var auth = services.GetRequiredService<ISupabaseAuthService>();
            if (!await auth.HasRememberedSessionAsync())
            {
                StartupTrace.Info(
                    "[LOG] Локальная сессия отсутствует или повреждена. Переход к LoginView.");
                ShowLoginWindow(desktop, services);
                return;
            }

            var restored = await auth.RestoreSessionAsync();
            StartupTrace.Info(
                $"[LOG] Результат RestoreSessionAsync: " +
                $"Success={restored.Success}, IsOffline={restored.IsOffline}, " +
                $"Error={restored.ErrorMessage ?? "<нет>"}");
            if (!restored.Success || restored.Session is null)
            {
                // RestoreSessionAsync reaches this point only when the protected
                // cache is invalid, expired, or fails its clock-integrity check.
                StartupTrace.Info(
                    $"[LOG] ShowLoginWindow: восстановление сессии отклонено. " +
                    $"Причина: {restored.ErrorMessage ?? "Session == null"}");
                ShowLoginWindow(desktop, services);
                return;
            }

            if (!restored.IsOffline)
            {
                try
                {
                    var persistence =
                        services.GetRequiredService<IApplicationPersistenceInitializer>();
                    await persistence.EnsureReadyAsync();
                }
                catch (Exception ex)
                {
                    // Supabase may be reachable while another online dependency
                    // is not. Keep the remembered user in the offline workspace.
                    StartupTrace.Error(
                        "[LOG] Инициализация online persistence: ошибка; переход в оффлайн",
                        ex);
                    restored = await auth.TryOfflineLoginFromCacheAsync();
                    if (!restored.Success || restored.Session is null)
                    {
                        StartupTrace.Info(
                            $"[LOG] ShowLoginWindow: offline fallback после ошибки persistence " +
                            $"не выполнен. Причина: {restored.ErrorMessage ?? "Session == null"}");
                        ShowLoginWindow(desktop, services);
                        return;
                    }
                }
            }

            var session = restored.Session;
            var account = new UserAccountItem
            {
                Id = session.UserId,
                Username = session.Email,
                GroupNumber = 1,
                Permissions = UserPermissions.CreateAdmin()
            };
            services.GetRequiredService<SettingsService>().CurrentUserId = account.Id;
            services.GetRequiredService<IUserSessionService>()
                .SetCurrentUser(account, restored.IsOffline);
            StartupTrace.Info("[LOG] Инициализация MainViewModel...");
            services.GetRequiredService<IWorkspaceWindowService>().OpenWorkspace();
            StartupTrace.Info(
                $"[LOG] Главное рабочее пространство открыто. " +
                $"Режим: {(restored.IsOffline ? "Оффлайн" : "Онлайн")}");
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            if (!restored.IsOffline)
            {
                _ = BootstrapSyncSafeAsync(services);
            }

            services.GetRequiredService<IBackgroundSynchronizer>().Start();
        }
        catch (Exception ex)
        {
            StartupTrace.Error(
                "[LOG] Необработанная ошибка startup pipeline; переход к LoginView",
                ex);
            ShowLoginWindow(desktop, Services);
        }
    }

    private static void ShowLoginWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        IServiceProvider services)
    {
        if (desktop.MainWindow is MainWindow)
        {
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            return;
        }

        var loginWindow = services.GetRequiredService<MainWindow>();
        StartupTrace.Info("[LOG] Создание и показ LoginView.");
        desktop.MainWindow = loginWindow;
        loginWindow.Show();
        desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

        services.GetRequiredService<IBackgroundSynchronizer>().Start();
    }

    private static async Task BootstrapSyncSafeAsync(IServiceProvider services)
    {
        try
        {
            await services.GetRequiredService<ISyncService>().BootstrapAfterLoginAsync();
        }
        catch (Exception ex)
        {
            StartupTrace.Error("[LOG] Bootstrap sync после авто-входа", ex);
        }
    }

}
