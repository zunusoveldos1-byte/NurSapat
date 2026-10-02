using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IOrderTemplateParametersService
{
    Task<bool> ShowAsync(Window? owner, OrderTemplateItem template);
}

public sealed class OrderTemplateParametersService(IServiceProvider serviceProvider) : IOrderTemplateParametersService
{
    public async Task<bool> ShowAsync(Window? owner, OrderTemplateItem template)
    {
        var viewModel = serviceProvider.GetRequiredService<OrderTemplateParametersViewModel>();
        viewModel.Load(template);

        var window = new OrderTemplateParametersWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();
        if (resolvedOwner is not null && await window.ShowDialog<bool?>(resolvedOwner) == true)
        {
            viewModel.ApplyTo(template);
            return true;
        }

        return false;
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
