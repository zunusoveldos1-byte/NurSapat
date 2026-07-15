using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Help;
using NurSapat.Infrastructure.Updates.Models;
using AppUpdateCheckResult = NurSapat.App.Services.Help.UpdateCheckResult;
using System.Globalization;

namespace NurSapat.App.ViewModels.Help;

public sealed partial class UpdateViewModel : HelpDialogViewModelBase
{
    private readonly IAppUpdateService _appUpdateService;
    private CancellationTokenSource? _operationCts;
    private UpdateReleaseInfo? _pendingRelease;

    public UpdateViewModel(IAppUpdateService appUpdateService)
    {
        _appUpdateService = appUpdateService;
        CurrentVersion = _appUpdateService.CurrentVersion;
        AvailableVersion = "—";
        UpdateCommand = new AsyncRelayCommand(InstallUpdateAsync, CanInstall);
        LaterCommand = new RelayCommand(Close);
        SkipVersionCommand = new RelayCommand(SkipVersion, () => _pendingRelease is not null);
        CancelDownloadCommand = new RelayCommand(CancelDownload, () => IsDownloading);
    }

    public IAsyncRelayCommand UpdateCommand { get; }

    public IRelayCommand LaterCommand { get; }

    public IRelayCommand SkipVersionCommand { get; }

    public IRelayCommand CancelDownloadCommand { get; }

    public string CurrentVersion { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelDownloadCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDeterminateProgress))]
    private bool _isUpdateProgressIndeterminate = true;

    [ObservableProperty]
    private double _downloadPercent;

    public bool ShowDeterminateProgress => IsDownloading && !IsUpdateProgressIndeterminate;

    [ObservableProperty]
    private string _availableVersion = "—";

    [ObservableProperty]
    private string _statusText = "Проверка обновлений...";

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _releaseNotes = string.Empty;

    [ObservableProperty]
    private string _publishedAtText = "—";

    [ObservableProperty]
    private string _packageSizeText = "—";

    [ObservableProperty]
    private string _downloadSpeedText = string.Empty;

    [ObservableProperty]
    private string _remainingTimeText = string.Empty;

    [ObservableProperty]
    private string _windowTitle = "Обновить программу";

    [ObservableProperty]
    private string _headingText = "Проверка обновлений";

    public async Task InitializeAsync() => await CheckForUpdatesAsync();

    public void ApplyCheckResult(AppUpdateCheckResult result)
    {
        ApplyResult(result);
        if (result.IsUpdateAvailable)
        {
            WindowTitle = "Доступно обновление";
            HeadingText = "Доступно обновление";
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        IsBusy = true;
        IsUpdateProgressIndeterminate = true;
        StatusText = "Проверка обновлений на сервере...";

        try
        {
            var result = await _appUpdateService.CheckForUpdatesAsync(force: true);
            ApplyResult(result);
        }
        finally
        {
            IsBusy = false;
            IsUpdateProgressIndeterminate = !IsDownloading;
        }
    }

    private void ApplyResult(AppUpdateCheckResult result)
    {
        _pendingRelease = result.Release;
        AvailableVersion = string.IsNullOrWhiteSpace(result.LatestVersion) ? "—" : result.LatestVersion;
        StatusText = result.Message;
        IsUpdateAvailable = result.IsUpdateAvailable;
        ReleaseNotes = string.IsNullOrWhiteSpace(result.ReleaseNotes)
            ? "Список изменений не указан."
            : result.ReleaseNotes;
        PublishedAtText = result.PublishedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "—";
        PackageSizeText = result.PackageSizeBytes is > 0
            ? FormatBytes(result.PackageSizeBytes.Value)
            : "—";

        if (result.IsUpdateAvailable)
        {
            WindowTitle = "Доступно обновление";
            HeadingText = "Доступно обновление";
        }

        SkipVersionCommand.NotifyCanExecuteChanged();
        UpdateCommand.NotifyCanExecuteChanged();
    }

    private bool CanInstall() => IsUpdateAvailable && !IsBusy && _pendingRelease is not null;

    private async Task InstallUpdateAsync()
    {
        if (_pendingRelease is null)
        {
            return;
        }

        _operationCts = new CancellationTokenSource();
        IsBusy = true;
        IsDownloading = true;
        IsUpdateProgressIndeterminate = false;
        StatusText = "Загрузка обновления...";

        var progress = new Progress<UpdateDownloadProgress>(report =>
        {
            DownloadPercent = report.PercentComplete;
            DownloadSpeedText = report.BytesPerSecond > 0
                ? $"{FormatBytes((long)report.BytesPerSecond)}/с"
                : string.Empty;
            RemainingTimeText = report.EstimatedTimeRemaining is { } eta && eta.TotalSeconds > 0
                ? $"Осталось: {FormatDuration(eta)}"
                : string.Empty;
            StatusText = report.Status;
        });

        try
        {
            var result = await _appUpdateService.DownloadAndApplyAsync(
                _pendingRelease,
                progress,
                _operationCts.Token);

            if (!result.Success)
            {
                StatusText = result.Message;
                IsDownloading = false;
                IsBusy = false;
                return;
            }

            StatusText = "Запуск установщика. Приложение будет закрыто.";
            await Task.Delay(300);
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Загрузка отменена.";
            IsDownloading = false;
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка обновления: {ex.Message}";
            IsDownloading = false;
        }
        finally
        {
            IsBusy = false;
            IsUpdateProgressIndeterminate = !IsDownloading;
            UpdateCommand.NotifyCanExecuteChanged();
            CancelDownloadCommand.NotifyCanExecuteChanged();
        }
    }

    private void CancelDownload()
    {
        _operationCts?.Cancel();
    }

    private void SkipVersion()
    {
        if (_pendingRelease is null)
        {
            return;
        }

        _appUpdateService.SkipVersion(_pendingRelease.Version);
        StatusText = $"Версия {_pendingRelease.Version} пропущена.";
        IsUpdateAvailable = false;
        CloseCommand.Execute(null);
    }

    private void Close() => CloseCommand.Execute(null);

    private static string FormatBytes(long bytes)
    {
        string[] units = ["Б", "КБ", "МБ", "ГБ"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}ч {duration.Minutes}м";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.Minutes}м {duration.Seconds}с";
        }

        return $"{Math.Max(1, duration.Seconds)}с";
    }
}
