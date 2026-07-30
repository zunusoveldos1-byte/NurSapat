using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.App.Services.Settings;
using NurSapat.App.Services.Utilities;
using NurSapat.App.Services.Documents;
using System.Collections.ObjectModel;
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
    private readonly IDocumentManager _documentManager;
    private readonly IDatabaseBackupPayloadService _databaseBackupPayloadService;

    private Func<Window?>? _ownerResolver;

    public UtilitiesViewModel(
        IDatabaseUtilitiesService databaseUtilitiesService,
        IDealerBaseService dealerBaseService,
        IProductionBaseService productionBaseService,
        IUtilitiesWindowService utilitiesWindowService,
        IEnvironmentSettingsService environmentSettingsService,
        IDialogService dialogService,
        IDocumentManager documentManager,
        IDatabaseBackupPayloadService databaseBackupPayloadService)
    {
        _databaseUtilitiesService = databaseUtilitiesService;
        _dealerBaseService = dealerBaseService;
        _productionBaseService = productionBaseService;
        _utilitiesWindowService = utilitiesWindowService;
        _environmentSettingsService = environmentSettingsService;
        _dialogService = dialogService;
        _documentManager = documentManager;
        _databaseBackupPayloadService = databaseBackupPayloadService;
    }

    public ObservableCollection<BackupArchiveItem> BackupArchives { get; } = [];

    [ObservableProperty]
    private BackupArchiveItem? _selectedBackupArchive;

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

    [RelayCommand]
    private async Task OpenDatabaseBackupsAsync()
    {
        RefreshBackupArchives();
        var owner = _ownerResolver?.Invoke();
        var window = new NurSapat.App.Views.Utilities.DatabaseBackupsWindow
        {
            DataContext = this
        };
        if (owner is null)
        {
            window.Show();
            return;
        }

        await window.ShowDialog(owner);
    }

    [RelayCommand]
    private async Task CreateDatabaseBackupAsync()
    {
        try
        {
            var directory = ResolveBackupDirectory();
            Directory.CreateDirectory(directory);
            var path = BuildUniqueDailyBackupPath(directory);
            if (!await _documentManager.CreateBackupAsync(path))
            {
                return;
            }

            RefreshBackupArchives();
            if (_ownerResolver?.Invoke() is { } owner)
            {
                AppNotificationHelper.ShowSuccess(
                    owner,
                    $"Резервная копия создана: {Path.GetFileName(path)}");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                $"Не удалось создать резервную копию: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseBackupAsync()
    {
        var selected = SelectedBackupArchive;
        if (selected is null)
        {
            await _dialogService.ShowErrorAsync("Выберите архив для восстановления.");
            return;
        }

        if (!File.Exists(selected.FullPath))
        {
            await _dialogService.ShowErrorAsync(
                new FileNotFoundException(
                    "Выбранный архив резервной копии не найден.",
                    selected.FullPath).Message);
            RefreshBackupArchives();
            return;
        }

        string? temporaryRestoreSource = null;
        try
        {
            var backupDirectory = ResolveBackupDirectory();
            Directory.CreateDirectory(backupDirectory);
            var nowPath = Path.Combine(backupDirectory, "now.zp");

            var restoreSource = selected.FullPath;
            if (Path.GetFullPath(restoreSource).Equals(
                    Path.GetFullPath(nowPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                temporaryRestoreSource = Path.Combine(
                    Path.GetTempPath(),
                    $"nursapat-restore-{Guid.NewGuid():N}.zp");
                File.Copy(restoreSource, temporaryRestoreSource, overwrite: true);
                restoreSource = temporaryRestoreSource;
            }

            if (!await _documentManager.CreateBackupAsync(nowPath))
            {
                return;
            }

            if (await _documentManager.RestorePreparedBackupAsync(restoreSource)
                && _ownerResolver?.Invoke() is { } owner)
            {
                AppNotificationHelper.ShowSuccess(owner, "Резервная копия восстановлена.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                $"Не удалось восстановить резервную копию: {ex.Message}");
        }
        finally
        {
            if (temporaryRestoreSource is not null)
            {
                try
                {
                    File.Delete(temporaryRestoreSource);
                }
                catch
                {
                    // Temporary cleanup must not hide restore errors.
                }
            }

            RefreshBackupArchives();
        }
    }

    [RelayCommand]
    private void RefreshBackupArchives()
    {
        BackupArchives.Clear();
        var directory = ResolveBackupDirectory();
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.zp")
                     .OrderByDescending(File.GetLastWriteTimeUtc))
        {
            var info = new FileInfo(path);
            BackupArchives.Add(new BackupArchiveItem(
                info.FullName,
                info.Name,
                info.LastWriteTime,
                info.Length));
        }
    }

    private string ResolveBackupDirectory()
    {
        var configured = _environmentSettingsService.Current.BackupPath;
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NurSapat",
                "Backups")
            : Path.GetFullPath(configured);
    }

    private static string BuildUniqueDailyBackupPath(string directory)
    {
        var baseName = DateTime.Now.ToString("yyyyMMdd");
        var path = Path.Combine(directory, $"{baseName}.zp");
        if (!File.Exists(path))
        {
            return path;
        }

        var timestamped = Path.Combine(
            directory,
            $"{baseName}_{DateTime.Now:HHmmss}.zp");
        if (!File.Exists(timestamped))
        {
            return timestamped;
        }

        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(directory, $"{baseName}_{index}.zp");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

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

public sealed record BackupArchiveItem(
    string FullPath,
    string FileName,
    DateTime CreatedAt,
    long SizeBytes)
{
    public string CreatedDisplay => CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");

    public string SizeDisplay => $"{SizeBytes / 1024d:N1} КБ";
}
