using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Account;
using NurSapat.App.Views.Account;

namespace NurSapat.App.Services.Users;

public interface IUserWindowService
{
    Task ShowChangePasswordAsync(Window? owner);
}

public sealed class UserWindowService(IServiceProvider serviceProvider) : IUserWindowService
{
    public async Task ShowChangePasswordAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ChangePasswordViewModel>();
        await ShowDialogSafeAsync(new ChangePasswordWindow(viewModel), owner);
    }

    private static async Task ShowDialogSafeAsync(Window window, Window? owner)
    {
        if (owner is not null)
        {
            await window.ShowDialog(owner);
            return;
        }

        window.Show();
    }
}
