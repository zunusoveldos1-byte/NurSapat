using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Stock;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Stock;

namespace NurSapat.App.Services.Stock;

public interface IGlassCutEditorService
{
    Task<GlassCutItem?> ShowEditorAsync(Window? owner, GlassCutItem source, bool isNew);
}

public sealed class GlassCutEditorService(IServiceProvider serviceProvider) : IGlassCutEditorService
{
    public async Task<GlassCutItem?> ShowEditorAsync(Window? owner, GlassCutItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<GlassCutEditViewModel>();
        await viewModel.InitializeAsync();
        viewModel.Load(source, isNew);

        var window = new GlassCutEditWindow(viewModel);
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
