using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IManufacturerEditorService
{
    Task<ManufacturerItem?> ShowEditorAsync(Window? owner, ManufacturerItem source);
}

public sealed class ManufacturerEditorService(IServiceProvider serviceProvider) : IManufacturerEditorService
{
    public async Task<ManufacturerItem?> ShowEditorAsync(Window? owner, ManufacturerItem source)
    {
        var viewModel = serviceProvider.GetRequiredService<ManufacturerEditViewModel>();
        viewModel.Load(source);

        var window = new ManufacturerEditWindow(viewModel);
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
