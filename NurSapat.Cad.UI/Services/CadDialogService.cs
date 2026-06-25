using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NurSapat.Cad.UI.Dialogs;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Services;

public sealed class CadDialogService : ICadDialogService
{
    public async Task<SizeInputResult?> ShowSizeInputAsync(
        double initialWidth,
        double initialHeight,
        double initialX = 0,
        double initialY = 0)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window owner)
        {
            return null;
        }

        var viewModel = new SizeInputViewModel(initialWidth, initialHeight, initialX, initialY);
        var window = new SizeInputWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new SizeInputResult(
            viewModel.ResultWidth,
            viewModel.ResultHeight,
            viewModel.ResultX,
            viewModel.ResultY);
    }

    public async Task<int?> ShowImpostCountAsync(int initialCount = 1)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window owner)
        {
            return null;
        }

        var viewModel = new ImpostCountViewModel(initialCount);
        var window = new ImpostCountWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return viewModel.ResultCount;
    }

    public async Task<CadDrawingTemplate?> ShowTemplatePickerAsync(CadTemplateStore store, CadEditorViewModel editor)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window owner)
        {
            return null;
        }

        var viewModel = new TemplatePickerViewModel(store, editor);
        var window = new TemplatePickerWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return viewModel.AppliedTemplate;
    }
}
