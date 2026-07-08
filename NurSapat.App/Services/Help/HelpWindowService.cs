using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Help;
using NurSapat.App.Views.Help;

namespace NurSapat.App.Services.Help;

public interface IHelpWindowService
{
    Task ShowAsync(Window? owner, string? tabKey = "Help");
}

public sealed class HelpWindowService(IServiceProvider serviceProvider) : IHelpWindowService
{
    public async Task ShowAsync(Window? owner, string? tabKey = "Help")
    {
        var viewModel = serviceProvider.GetRequiredService<HelpAndAboutViewModel>();
        viewModel.SetInitialTab(tabKey);
        await ShowDialogSafeAsync(new HelpAndAboutWindow(viewModel), owner);
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
