using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Help;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Help;

public sealed partial class UpdateViewModel : HelpDialogViewModelBase
{
    private readonly IAppUpdateService _appUpdateService;

    public UpdateViewModel(IAppUpdateService appUpdateService)
    {
        _appUpdateService = appUpdateService;
        CurrentVersion = _appUpdateService.CurrentVersion;
        AvailableVersion = "—";
        UpdateCommand = new AsyncRelayCommand(UpdateAsync, () => !IsUpdating);
    }

    public IAsyncRelayCommand UpdateCommand { get; }

    public string CurrentVersion { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    private bool _isUpdating;

    [ObservableProperty]
    private bool _isUpdateProgressIndeterminate;

    [ObservableProperty]
    private string _availableVersion = "—";

    [ObservableProperty]
    private string _statusText = "Нажмите «Обновить», чтобы проверить наличие новой версии.";

    [ObservableProperty]
    private bool _isUpdateAvailable;

    private string? _downloadUrl;

    public async Task InitializeAsync() => await CheckForUpdatesAsync(openDownloadIfAvailable: false);

    private async Task UpdateAsync() => await CheckForUpdatesAsync(openDownloadIfAvailable: true);

    private async Task CheckForUpdatesAsync(bool openDownloadIfAvailable)
    {
        IsUpdating = true;
        IsUpdateProgressIndeterminate = true;
        StatusText = "Проверка обновлений на сервере...";
        _downloadUrl = null;
        IsUpdateAvailable = false;
        AvailableVersion = "—";

        try
        {
            var result = await _appUpdateService.CheckForUpdatesAsync();
            _downloadUrl = result.DownloadUrl;
            AvailableVersion = string.IsNullOrWhiteSpace(result.LatestVersion) ? "—" : result.LatestVersion;
            StatusText = result.Message;
            IsUpdateAvailable = result.IsUpdateAvailable;

            if (result.IsUpdateAvailable)
            {
                StatusText += " Нажмите «Обновить», чтобы открыть страницу загрузки.";

                if (openDownloadIfAvailable && !string.IsNullOrWhiteSpace(result.DownloadUrl))
                {
                    OpenDownloadPage();
                }
            }
        }
        finally
        {
            IsUpdating = false;
            IsUpdateProgressIndeterminate = false;
        }
    }

    private void OpenDownloadPage()
    {
        if (!string.IsNullOrWhiteSpace(_downloadUrl))
        {
            Process.Start(new ProcessStartInfo(_downloadUrl) { UseShellExecute = true });
        }
    }
}
