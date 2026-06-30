using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using NurSapat.App.ViewModels;
using NurSapat.App.Views.Dialogs;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services;

public sealed class DialogService : IDialogService
{
    private static readonly FilePickerFileType ProjectFileType = new(ProjectFormat.FilterName)
    {
        Patterns = new[] { $"*{ProjectFormat.Extension}" },
        MimeTypes = new[] { "application/vnd.nursapat.project+json" }
    };

    public async Task<UnsavedChangesChoice> ShowUnsavedChangesDialogAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return UnsavedChangesChoice.Cancel;
        }

        var viewModel = new UnsavedChangesViewModel(title, message);
        var window = new UnsavedChangesWindow { DataContext = viewModel };
        viewModel.CloseRequested += (_, choice) => window.Close(choice);

        var result = await window.ShowDialog<UnsavedChangesChoice?>(owner);
        return result ?? UnsavedChangesChoice.Cancel;
    }

    public async Task<YesNoChoice> ShowAutoSaveRecoveryDialogAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return YesNoChoice.No;
        }

        var viewModel = new AutoSaveRecoveryViewModel();
        var window = new AutoSaveRecoveryWindow { DataContext = viewModel };
        viewModel.CloseRequested += (_, choice) => window.Close(choice);

        var result = await window.ShowDialog<YesNoChoice?>(owner);
        return result ?? YesNoChoice.No;
    }

    public async Task<string?> ShowSaveFileDialogAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        var storage = GetStorageProvider();
        if (storage is null)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить проект NurSapat",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = ProjectFormat.Extension.TrimStart('.'),
            FileTypeChoices = new[] { ProjectFileType }
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> ShowOpenFileDialogAsync(CancellationToken cancellationToken = default)
    {
        var storage = GetStorageProvider();
        if (storage is null)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть проект NurSapat",
            AllowMultiple = false,
            FileTypeFilter = new[] { ProjectFileType }
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task ShowErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return;
        }

        var dialog = new Window
        {
            Title = "Ошибка",
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => { })
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children[1] is Button button)
        {
            button.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => dialog.Close());
        }

        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ShowPrintDialogAsync(PrintDialogViewModel viewModel, CancellationToken cancellationToken = default)
    {
        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return false;
        }

        var window = new PrintDialogWindow { DataContext = viewModel };
        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        return await window.ShowDialog<bool?>(owner) == true;
    }

    public async Task<bool> ShowPrintSettingsDialogAsync(PrintSettingsViewModel viewModel, CancellationToken cancellationToken = default)
    {
        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return false;
        }

        var window = new PrintSettingsWindow { DataContext = viewModel };
        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        return await window.ShowDialog<bool?>(owner) == true;
    }

    private static Window? GetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private static IStorageProvider? GetStorageProvider() => GetOwnerWindow()?.StorageProvider;
}
