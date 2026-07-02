using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Stock;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Stock;

namespace NurSapat.App.Services.Stock;

public interface IFinishedGoodEditorService
{
    Task<FinishedGoodItem?> ShowEditorAsync(Window? owner, FinishedGoodItem source, bool isNew);
}

public sealed class FinishedGoodEditorService(IServiceProvider serviceProvider) : IFinishedGoodEditorService
{
    public async Task<FinishedGoodItem?> ShowEditorAsync(Window? owner, FinishedGoodItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<FinishedGoodEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new FinishedGoodEditWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();

        bool? result = resolvedOwner is not null
            ? await window.ShowDialog<bool?>(resolvedOwner)
            : null;

        return result == true ? viewModel.BuildResult() : null;
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
