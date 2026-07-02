using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Stock;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Stock;

namespace NurSapat.App.Services.Stock;

public interface IStockDocumentEditorService
{
    Task<StockDocumentHeader?> ShowEditorAsync(Window? owner, StockDocumentHeader source, bool isNew);
}

public sealed class StockDocumentEditorService(IServiceProvider serviceProvider) : IStockDocumentEditorService
{
    public async Task<StockDocumentHeader?> ShowEditorAsync(Window? owner, StockDocumentHeader source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<StockDocumentEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new StockDocumentEditWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();

        bool? result = resolvedOwner is not null
            ? await window.ShowDialog<bool?>(resolvedOwner)
            : await ShowWithoutOwnerAsync(window, viewModel);

        return result == true ? viewModel.BuildResult() : null;
    }

    private static async Task<bool?> ShowWithoutOwnerAsync(StockDocumentEditWindow window, StockDocumentEditViewModel viewModel)
    {
        var completion = new TaskCompletionSource<bool?>();

        void OnRequestClose(bool value)
        {
            viewModel.RequestClose -= OnRequestClose;
            completion.TrySetResult(value);
        }

        window.Closed += (_, _) => completion.TrySetResult(false);
        viewModel.RequestClose += OnRequestClose;
        window.Show();

        return await completion.Task;
    }

    private static Window? ResolveMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}
