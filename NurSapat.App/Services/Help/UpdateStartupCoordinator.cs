using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using NurSapat.App.Views;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Updates.Abstractions;

namespace NurSapat.App.Services.Help;

public sealed class UpdateStartupCoordinator
{
    private readonly IAppUpdateService _appUpdateService;
    private readonly IUpdateWindowService _updateWindowService;
    private readonly IUpdateSettingsService _settingsService;
    private int _promptInFlight;
    private bool _exiting;

    public UpdateStartupCoordinator(
        IAppUpdateService appUpdateService,
        IUpdateWindowService updateWindowService,
        IUpdateSettingsService settingsService)
    {
        _appUpdateService = appUpdateService;
        _updateWindowService = updateWindowService;
        _settingsService = settingsService;
    }

    public void Initialize(string[] args)
    {
        _appUpdateService.UpdateAvailable += OnUpdateAvailable;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) =>
            {
                _exiting = true;
                _appUpdateService.UpdateAvailable -= OnUpdateAvailable;
            };
        }
        _appUpdateService.StartPeriodicChecks();
        _ = HandleStartupAsync(args);
    }

    private async Task HandleStartupAsync(string[] args)
    {
        await _settingsService.LoadAsync();

        var successVersion = ParseSuccessArgument(args)
            ?? _settingsService.Current.PendingSuccessVersion;

        if (!string.IsNullOrWhiteSpace(successVersion))
        {
            _settingsService.ClearPendingSuccessVersion();
            if (Version.TryParse(successVersion, out var version))
            {
                _settingsService.RecordSuccessfulUpdate(version);
            }

            await _settingsService.SaveAsync();
            await ShowSuccessPromptAsync(successVersion);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _appUpdateService.CheckForUpdatesAsync();
            }
            catch
            {
                // errors are logged by update service
            }
        });
    }

    private async void OnUpdateAvailable(object? sender, UpdateCheckResult result)
    {
        if (!result.IsUpdateAvailable || result.Release is null)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _promptInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowUpdateInWorkspaceAsync(result));
        }
        catch (Exception ex)
        {
            StartupTrace.Error("Не удалось показать уведомление об обновлении.", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _promptInFlight, 0);
        }
    }

    private async Task ShowUpdateInWorkspaceAsync(UpdateCheckResult result)
    {
        // The login window is replaced when the workspace opens.
        while (!_exiting)
        {
            if (GetActiveWindow() is MainWorkspaceWindow { IsVisible: true } workspace)
            {
                await _updateWindowService.ShowAvailableUpdateAsync(workspace, result);
                return;
            }

            await Task.Delay(200);
        }
    }

    private async Task ShowSuccessPromptAsync(string version)
    {
        await Task.Delay(500);
        var owner = GetActiveWindow() ?? await WaitForMainWindowAsync(TimeSpan.FromSeconds(10));
        await _updateWindowService.ShowSuccessPromptAsync(owner, version);
    }

    private static Window? GetActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private static async Task<Window?> WaitForMainWindowAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var main = GetActiveWindow();
            if (main is not null)
            {
                return main;
            }

            await Task.Delay(100);
        }

        return GetActiveWindow();
    }

    private static string? ParseSuccessArgument(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--update-success", StringComparison.OrdinalIgnoreCase))
            {
                var parts = arg.Split('=', 2);
                return parts.Length > 1 ? parts[1] : null;
            }
        }

        return null;
    }
}
