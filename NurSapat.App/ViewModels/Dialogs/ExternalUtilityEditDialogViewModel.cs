using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Settings;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class ExternalUtilityEditDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Редактор утилиты";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    public Window? OwnerWindow { get; set; }

    public event Action<ExternalUtilityItem?>? RequestClose;

    public void Load(ExternalUtilityItem? source, bool isNew)
    {
        Title = isNew ? "Добавить утилиту" : "Редактировать утилиту";
        if (source is null)
        {
            Name = string.Empty;
            FilePath = string.Empty;
            return;
        }

        Name = source.Name;
        FilePath = source.FilePath;
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storage)
        {
            return;
        }

        var exeFilter = new FilePickerFileType("Исполняемые файлы")
        {
            Patterns = ["*.exe", "*.EXE"],
            MimeTypes = ["application/octet-stream"]
        };

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите исполняемый файл",
            AllowMultiple = false,
            FileTypeFilter = [exeFilter]
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
            {
                FilePath = path;
            }
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(FilePath))
        {
            return;
        }

        RequestClose?.Invoke(new ExternalUtilityItem
        {
            Name = Name.Trim(),
            FilePath = FilePath.Trim()
        });
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
