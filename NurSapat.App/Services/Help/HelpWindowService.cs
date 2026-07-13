using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Help;
using NurSapat.App.ViewModels.Help;
using NurSapat.App.Views.Help;

namespace NurSapat.App.Services.Help;

public interface IHelpWindowService
{
    Task ShowAsync(Window? owner, string? tabKey = HelpTabKeys.Help);
}

public sealed class HelpWindowService(IServiceProvider serviceProvider) : IHelpWindowService
{
    public async Task ShowAsync(Window? owner, string? tabKey = HelpTabKeys.Help)
    {
        var window = CreateWindow(HelpTabKeys.Parse(tabKey));
        await ShowDialogSafeAsync(window, owner);
    }

    private Window CreateWindow(HelpTab tab) => tab switch
    {
        HelpTab.WhatsNew => Create<WhatsNewWindow, WhatsNewViewModel>(),
        HelpTab.Update => Create<UpdateWindow, UpdateViewModel>(),
        HelpTab.Consultation => Create<SupportWindow, SupportViewModel>(),
        HelpTab.About => Create<AboutWindow, AboutViewModel>(),
        _ => Create<HelpWindow, HelpGuideViewModel>()
    };

    private TWindow Create<TWindow, TViewModel>()
        where TWindow : Window
        where TViewModel : notnull
    {
        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        return (TWindow)Activator.CreateInstance(typeof(TWindow), viewModel)!;
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
