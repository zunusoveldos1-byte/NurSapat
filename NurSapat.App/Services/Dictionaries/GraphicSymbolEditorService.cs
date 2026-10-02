using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IGraphicSymbolEditorService
{
    Task<GraphicSymbolItem?> ShowEditorAsync(Window? owner, GraphicSymbolItem source, bool isNew);
}

public sealed class GraphicSymbolEditorService(IServiceProvider serviceProvider) : IGraphicSymbolEditorService
{
    public async Task<GraphicSymbolItem?> ShowEditorAsync(Window? owner, GraphicSymbolItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<GraphicSymbolEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new GraphicSymbolEditWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();
        var result = resolvedOwner is not null
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
