using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Orders;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Orders;

public interface IOrderHeaderEditorService
{
    Task<OrderHeaderItem?> ShowEditorAsync(Window? owner, OrderHeaderItem source, bool isNew);
}

public sealed class OrderHeaderEditorService(IServiceProvider serviceProvider) : IOrderHeaderEditorService
{
    public async Task<OrderHeaderItem?> ShowEditorAsync(Window? owner, OrderHeaderItem source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<OrderHeaderEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new OrderHeaderEditWindow(viewModel);
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
