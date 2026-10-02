using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.App.Services.Settings;
using NurSapat.App.Services.Utilities;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Utilities;

public sealed partial class UtilitiesViewModel : ViewModelBase
{
    private readonly IDatabaseUtilitiesService _databaseUtilitiesService;
    private readonly IDealerBaseService _dealerBaseService;
    private readonly IProductionBaseService _productionBaseService;
    private readonly IUtilitiesWindowService _utilitiesWindowService;
    private readonly IEnvironmentSettingsService _environmentSettingsService;
    private readonly IDialogService _dialogService;

    private Func<Window?>? _ownerResolver;

    public UtilitiesViewModel(
        IDatabaseUtilitiesService databaseUtilitiesService,
        IDealerBaseService dealerBaseService,
        IProductionBaseService productionBaseService,
        IUtilitiesWindowService utilitiesWindowService,
        IEnvironmentSettingsService environmentSettingsService,
        IDialogService dialogService)
    {
        _databaseUtilitiesService = databaseUtilitiesService;
        _dealerBaseService = dealerBaseService;
        _productionBaseService = productionBaseService;
        _utilitiesWindowService = utilitiesWindowService;
        _environmentSettingsService = environmentSettingsService;
        _dialogService = dialogService;
    }

    public void ConfigureOwnerResolver(Func<Window?> ownerResolver) =>
        _ownerResolver = ownerResolver;

    [RelayCommand]
    private void LaunchCalculator()
    {
        var path = _environmentSettingsService.Current.CalculatorPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe");
        }

        if (!File.Exists(path))
        {
            _ = _dialogService.ShowErrorAsync($"Калькулятор не найден: {path}");
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task CompressDatabaseAsync()
    {
        var owner = _ownerResolver?.Invoke();
        Exception? error = null;

        await _utilitiesWindowService.ShowCompressProgressAsync(
            owner,
            async cancellationToken =>
            {
                try
                {
                    await _databaseUtilitiesService.CompressAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

        if (error is not null)
        {
            await _dialogService.ShowErrorAsync($"Не удалось сжать базу: {error.Message}");
            return;
        }

        if (owner is not null)
        {
            AppNotificationHelper.ShowSuccess(owner, "База успешно сжата!");
        }
    }

    [RelayCommand]
    private async Task CreateDealerBaseAsync() =>
        await _utilitiesWindowService.ShowDealerBaseAsync(_ownerResolver?.Invoke());

    [RelayCommand]
    private async Task SetDatabasePasswordAsync() =>
        await _utilitiesWindowService.ShowSetDatabasePasswordAsync(_ownerResolver?.Invoke());

    [RelayCommand]
    private void OpenCorporateNode()
    {
        var path = _environmentSettingsService.Current.BackupPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NurSapat",
                "CorporateNode");
        }

        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task ExportProductionBaseAsync()
    {
        var owner = _ownerResolver?.Invoke();
        var path = await ShowSaveProductionFileAsync(owner);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _productionBaseService.ExportUpdatePackageAsync(path);
            if (owner is not null)
            {
                AppNotificationHelper.ShowSuccess(owner, "Производственная база для обновления сформирована.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Не удалось сформировать пакет: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportProductionBaseAsync() =>
        await _utilitiesWindowService.ShowProductionBaseImportAsync(_ownerResolver?.Invoke());

    private static async Task<string?> ShowSaveProductionFileAsync(Window? owner)
    {
        if (owner?.StorageProvider is not { } storage)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сформировать производственную базу",
            SuggestedFileName = "production-update.prod-update",
            DefaultExtension = "prod-update",
            FileTypeChoices =
            [
                new FilePickerFileType("Пакет обновления NurSapat")
                {
                    Patterns = ["*.prod-update"]
                }
            ]
        });

        return file?.TryGetLocalPath();
    }
}
