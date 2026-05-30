using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Settings;
using NurSapat.Data;

namespace NurSapat.App.ViewModels;

public sealed partial class DataTransferViewModel : ObservableObject
{
    private readonly DataMigrationService _dataMigrationService;
    private readonly EnvironmentSettings _environmentSettings;

    [ObservableProperty]
    private string _importFilePath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Выберите операцию коммерческого переноса данных.";

    [ObservableProperty]
    private bool _isBusy;

    public DataTransferViewModel(
        DataMigrationService dataMigrationService,
        EnvironmentSettings environmentSettings)
    {
        _dataMigrationService = dataMigrationService;
        _environmentSettings = environmentSettings;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        IsBusy = true;
        try
        {
            var fileName = $"NurSapat-export-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var targetPath = Path.Combine(_environmentSettings.DocumentsPath, fileName);
            await _dataMigrationService.ExportDataAsync(targetPath);
            StatusMessage = $"Экспорт завершен: {targetPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Экспорт не выполнен: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportFilePath))
        {
            StatusMessage = "Укажите путь к ZIP или JSON-файлу для импорта.";
            return;
        }

        IsBusy = true;
        try
        {
            await _dataMigrationService.ImportDataAsync(ImportFilePath);
            StatusMessage = $"Импорт завершен: {ImportFilePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Импорт не выполнен: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
