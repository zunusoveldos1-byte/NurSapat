using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Export;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Export;

public sealed partial class ExportXlsViewModel : ViewModelBase
{
    private readonly IExportService _exportService;
    private readonly OrderWorkspaceViewModel _workspace;

    public ExportXlsViewModel(
        IExportService exportService,
        OrderWorkspaceViewModel workspace)
    {
        _exportService = exportService;
        _workspace = workspace;

        BrowseFolderCommand = new AsyncRelayCommand(BrowseFolderAsync);
        ExportXlsCommand = new AsyncRelayCommand(ExportAsync, () => !IsExporting);
        OpenFileCommand = new RelayCommand(OpenExportedFile, () => CanOpenFile);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());

        ExportFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DATA");
    }

    public IAsyncRelayCommand BrowseFolderCommand { get; }

    public IAsyncRelayCommand ExportXlsCommand { get; }

    public IRelayCommand OpenFileCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private string _exportFolderPath;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isProgressVisible;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string? _lastExportedFilePath;

    [ObservableProperty]
    private bool _canOpenFile;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(ExportFolderPath);
        return Task.CompletedTask;
    }

    partial void OnCanOpenFileChanged(bool value) => OpenFileCommand.NotifyCanExecuteChanged();

    private async Task BrowseFolderAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storageProvider)
        {
            return;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку для экспорта Excel",
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
            CanOpenFile = false;
            ExportXlsCommand.NotifyCanExecuteChanged();

            _workspace.CommitActiveConstructionGeometry();

            var progress = new Progress<int>(value => ProgressValue = value);
            var result = await _exportService.ExportXlsAsync(
                _workspace.Order,
                ExportFolderPath,
                progress);

            if (!result.Success)
            {
                if (OwnerWindow is not null)
                {
                    ExportNotificationHelper.ShowWarning(OwnerWindow, result.Message);
                }

                return;
            }

            LastExportedFilePath = result.PrimaryFilePath;
            CanOpenFile = !string.IsNullOrWhiteSpace(LastExportedFilePath) && File.Exists(LastExportedFilePath);

            if (OwnerWindow is not null)
            {
                ExportNotificationHelper.ShowSuccess(OwnerWindow, "Экспорт успешно завершен!");
            }
        }
        catch (Exception ex)
        {
            if (OwnerWindow is not null)
            {
                ExportNotificationHelper.ShowWarning(OwnerWindow, $"Ошибка экспорта: {ex.Message}");
            }
        }
        finally
        {
            IsExporting = false;
            ExportXlsCommand.NotifyCanExecuteChanged();
        }
    }

    private void OpenExportedFile()
    {
        if (string.IsNullOrWhiteSpace(LastExportedFilePath) || !File.Exists(LastExportedFilePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = LastExportedFilePath,
            UseShellExecute = true
        });
    }
}
