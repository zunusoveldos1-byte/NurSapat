using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Structures;
using NurSapat.App.ViewModels.Structures;
using NurSapat.App.Views.Structures;

namespace NurSapat.App.Services.Structures;

public interface IStandardStructureEditorService
{
    Task<StandardStructureGroup?> EditGroupAsync(Window? owner, StandardStructureGroup source, bool isNew);

    Task<StandardStructureItem?> EditStructureAsync(Window? owner, StandardStructureItem source, bool isNew);
}

public sealed class StandardStructureEditorService(IServiceProvider serviceProvider) : IStandardStructureEditorService
{
    public async Task<StandardStructureGroup?> EditGroupAsync(Window? owner, StandardStructureGroup source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<StandardStructureGroupEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new StandardStructureGroupEditWindow(viewModel);
        return await ShowDialogAsync(window, viewModel, owner);
    }

    public async Task<StandardStructureItem?> EditStructureAsync(Window? owner, StandardStructureItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<StandardStructureEditViewModel>();
        await viewModel.InitializeAsync();
        viewModel.Load(source, isNew);

        var window = new StandardStructureEditWindow(viewModel);
        return await ShowDialogAsync(window, viewModel, owner);
    }

    private static async Task<T?> ShowDialogAsync<T>(Window window, IEditDialogResult<T> viewModel, Window? owner)
        where T : class
    {
        var resolvedOwner = owner ?? ResolveMainWindow();
        if (resolvedOwner is null)
        {
            return null;
        }

        var result = await window.ShowDialog<bool?>(resolvedOwner);
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

public interface IEditDialogResult<T>
    where T : class
{
    T BuildResult();
}
