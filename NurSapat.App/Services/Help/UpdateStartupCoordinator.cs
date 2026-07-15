using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Help;
using NurSapat.Infrastructure.Updates.Abstractions;

namespace NurSapat.App.Services.Help;

public sealed class UpdateStartupCoordinator
{
    private readonly IAppUpdateService _appUpdateService;
    private readonly IUpdateWindowService _updateWindowService;
    private readonly IUpdateSettingsService _settingsService;

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

        var owner = GetActiveWindow();
        await _updateWindowService.ShowAvailableUpdateAsync(owner, result);
    }

    private async Task ShowSuccessPromptAsync(string version)
    {
        await Task.Delay(500);
        var owner = GetActiveWindow();
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
