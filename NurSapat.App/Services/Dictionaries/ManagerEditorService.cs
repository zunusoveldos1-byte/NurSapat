using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IManagerEditorService
{
    Task<ManagerItem?> ShowEditorAsync(Window? owner, ManagerItem source, bool isNew);
}

public sealed class ManagerEditorService(IServiceProvider serviceProvider) : IManagerEditorService
{
    public async Task<ManagerItem?> ShowEditorAsync(Window? owner, ManagerItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<ManagerEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new ManagerEditWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();

        bool? result = resolvedOwner is not null
            ? await window.ShowDialog<bool?>(resolvedOwner)
            : await ShowWithoutOwnerAsync(window, viewModel);

        return result == true ? viewModel.BuildResult() : null;
    }

    private static async Task<bool?> ShowWithoutOwnerAsync(ManagerEditWindow window, ManagerEditViewModel viewModel)
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
