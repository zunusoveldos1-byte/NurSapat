using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Export;
using NurSapat.Cad.UI.ViewModels;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Export;

public sealed partial class ExportDbfJsonViewModel : ViewModelBase
{
    private readonly IExportService _exportService;
    private readonly OrderWorkspaceViewModel _workspace;
    private readonly CadEditorViewModel _cadEditor;

    public ExportDbfJsonViewModel(
        IExportService exportService,
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor)
    {
        _exportService = exportService;
        _workspace = workspace;
        _cadEditor = cadEditor;

        BrowseFolderCommand = new AsyncRelayCommand(BrowseFolderAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync, () => !IsExporting);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());

        ExportFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DATA");
    }

    public IAsyncRelayCommand BrowseFolderCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IRelayCommand OpenDataFolderCommand { get; }

    public IRelayCommand CloseCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private string _exportFolderPath;

    [ObservableProperty]
    private bool _exportJsonSnapshot = true;

    [ObservableProperty]
    private bool _exportDbfFiles;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isProgressVisible;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string _statusText = "Готов к экспорту";

    public Task InitializeAsync()
    {
        UpdateStatusText();
        Directory.CreateDirectory(ExportFolderPath);
        return Task.CompletedTask;
    }

    partial void OnExportJsonSnapshotChanged(bool value)
    {
        if (value)
        {
            ExportDbfFiles = false;
        }
        else if (!ExportDbfFiles)
        {
            ExportDbfFiles = true;
        }
    }

    partial void OnExportDbfFilesChanged(bool value)
    {
        if (value)
        {
            ExportJsonSnapshot = false;
        }
        else if (!ExportJsonSnapshot)
        {
            ExportJsonSnapshot = true;
        }
    }

    private void UpdateStatusText()
    {
        var orderNumber = string.IsNullOrWhiteSpace(_workspace.Order.Header.OrderNumber)
            ? "—"
            : _workspace.Order.Header.OrderNumber.Trim();
        StatusText = $"Готов к экспорту заказа № {orderNumber}";
    }

    private async Task BrowseFolderAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storageProvider)
        {
            return;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку для экспорта",
            AllowMultiple = false
        });

        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path)
        {
            ExportFolderPath = path;
        }
    }

    private async Task ExportAsync()
    {
        if (IsExporting)
        {
            return;
        }

        try
        {
            IsExporting = true;
            IsProgressVisible = true;
            ProgressValue = 0;
            ExportCommand.NotifyCanExecuteChanged();

            _workspace.CommitActiveConstructionGeometry();

            var progress = new Progress<int>(value => ProgressValue = value);
            ExportResult result;

            if (ExportJsonSnapshot)
            {
                StatusText = "Формирование JSON-снимка заказа...";
                result = await _exportService.ExportJsonAsync(
                    _workspace.Order,
                    _cadEditor,
                    ExportFolderPath,
                    progress);
            }
            else
            {
                StatusText = "Формирование файлов dBASE 5.0...";
                result = await _exportService.ExportDbfAsync(
                    _workspace.Order,
                    ExportFolderPath,
                    progress);
            }

            if (!result.Success)
            {
                StatusText = result.Message;
                if (OwnerWindow is not null)
                {
                    ExportNotificationHelper.ShowWarning(OwnerWindow, result.Message);
                }

                return;
            }

            StatusText = result.Message;
            if (OwnerWindow is not null)
            {
                ExportNotificationHelper.ShowSuccess(OwnerWindow, "Экспорт успешно завершен!");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка экспорта: {ex.Message}";
            if (OwnerWindow is not null)
            {
                ExportNotificationHelper.ShowWarning(OwnerWindow, StatusText);
            }
        }
        finally
        {
            IsExporting = false;
            ExportCommand.NotifyCanExecuteChanged();
            UpdateStatusText();
        }
    }

    private void OpenDataFolder()
    {
        Directory.CreateDirectory(ExportFolderPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = ExportFolderPath,
            UseShellExecute = true
        });
    }
}
