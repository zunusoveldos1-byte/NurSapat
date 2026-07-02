using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Users;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Users;

public interface IUserEditorService
{
    Task<UserAccountItem?> ShowEditorAsync(Window? owner, UserAccountItem source, bool isNew);
}

public sealed class UserEditorService(IServiceProvider serviceProvider) : IUserEditorService
{
    public async Task<UserAccountItem?> ShowEditorAsync(Window? owner, UserAccountItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<UserEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new UserEditWindow(viewModel);
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
