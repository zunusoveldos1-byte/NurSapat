using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Help;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;
using System.Globalization;

namespace NurSapat.App.ViewModels.Settings;

public sealed partial class UpdateSettingsViewModel : ViewModelBase
{
    private readonly IAppUpdateService _appUpdateService;
    private readonly IUpdateSettingsService _settingsService;

    public UpdateSettingsViewModel(
        IAppUpdateService appUpdateService,
        IUpdateSettingsService settingsService)
    {
        _appUpdateService = appUpdateService;
        _settingsService = settingsService;
        ChannelOptions =
        [
            new UpdateChannelOption(UpdateChannel.Stable, "Stable"),
            new UpdateChannelOption(UpdateChannel.Beta, "Beta"),
            new UpdateChannelOption(UpdateChannel.Development, "Development")
        ];
        CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdatesAsync);
        ApplyFromSettings();
    }

    public IReadOnlyList<UpdateChannelOption> ChannelOptions { get; }

    public IAsyncRelayCommand CheckUpdatesCommand { get; }

    [ObservableProperty]
    private string _currentVersion = string.Empty;

    [ObservableProperty]
    private string _latestVersion = "—";

    [ObservableProperty]
    private string _lastCheckText = "—";

    [ObservableProperty]
    private string _lastUpdateText = "—";

    [ObservableProperty]
    private bool _autoCheckEnabled = true;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private UpdateChannelOption? _selectedChannelOption;

    partial void OnSelectedChannelOptionChanged(UpdateChannelOption? value)
    {
        if (value is null)
        {
            return;
        }

        _settingsService.Current.Channel = value.Channel;
        _ = SaveAsync();
    }

    partial void OnAutoCheckEnabledChanged(bool value)
    {
        _settingsService.Current.AutoCheckEnabled = value;
        _ = SaveAsync();
    }

    private void ApplyFromSettings()
    {
        var settings = _settingsService.Current;
        CurrentVersion = _appUpdateService.CurrentVersion;
        LatestVersion = settings.LastKnownLatestVersion ?? "—";
        LastCheckText = settings.LastCheckUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "—";
        LastUpdateText = settings.LastUpdateUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "—";
        AutoCheckEnabled = settings.AutoCheckEnabled;
        SelectedChannelOption = ChannelOptions.FirstOrDefault(option => option.Channel == settings.Channel)
            ?? ChannelOptions[0];
    }

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        ApplyFromSettings();
    }

    private async Task CheckUpdatesAsync()
    {
        IsChecking = true;
        StatusText = "Проверка обновлений...";

        try
        {
            var result = await _appUpdateService.CheckForUpdatesAsync(force: true);
            LatestVersion = result.LatestVersion ?? "—";
            LastCheckText = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
            StatusText = result.Message;
            await _settingsService.SaveAsync();
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task SaveAsync() => await _settingsService.SaveAsync();
}

public sealed record UpdateChannelOption(UpdateChannel Channel, string Title);
