using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.Views.Dialogs;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;
using NurSapat.Core.Interfaces.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace NurSapat.App.Services.Orders;

public interface IOrderErpDialogService
{
    Task<bool> ShowOrderSettingsAsync(Order order, bool isAdministrator);

    Task<bool> ShowDealerSelectAsync(Order order);

    Task<bool> ShowPriceModeAsync(Order order);

    Task<bool> ShowConfirmDeleteAsync(string confirmationMessage, string? title = null);
}

public sealed class OrderErpDialogService(
    IDealersCatalog dealersCatalog,
    IServiceProvider serviceProvider,
    OrderLifecycleService lifecycleService) : IOrderErpDialogService
{
    public async Task<bool> ShowOrderSettingsAsync(Order order, bool isAdministrator)
    {
        var snapshotRepository = serviceProvider.GetService<IOrderCalculationSnapshotRepository>();
        var hasSnapshot = snapshotRepository is not null
            && Persistence.PersistenceSync.Run(
                () => snapshotRepository.GetLatestByOrderIdAsync(order.Id)) is not null;
        var viewModel = new OrderSettingsDialogViewModel(
            order,
            isAdministrator,
            dealersCatalog,
            lifecycleService,
            hasSnapshot);
        var window = new OrderSettingsDialogWindow { DataContext = viewModel };
        return await ShowAsync(window, viewModel) == true;
    }

    public async Task<bool> ShowDealerSelectAsync(Order order)
    {
        var viewModel = new DealerSelectDialogViewModel(order, dealersCatalog);
        var window = new DealerSelectDialogWindow { DataContext = viewModel };
        return await ShowAsync(window, viewModel) == true;
    }

    public async Task<bool> ShowPriceModeAsync(Order order)
    {
        var viewModel = new PriceModeDialogViewModel(order);
        var window = new PriceModeDialogWindow { DataContext = viewModel };
        return await ShowAsync(window, viewModel) == true;
    }

    public async Task<bool> ShowConfirmDeleteAsync(string confirmationMessage, string? title = null)
    {
        var viewModel = new ConfirmDeleteDialogViewModel(confirmationMessage, title ?? "Удаление конструкции");
        var window = new ConfirmDeleteDialogWindow { DataContext = viewModel };
        return await ShowAsync(window, viewModel) == true;
    }

    private static async Task<bool?> ShowAsync(Window window, object viewModel)
    {
        if (viewModel is OrderSettingsDialogViewModel orderSettings)
        {
            orderSettings.CloseRequested += result => window.Close(result);
        }
        else if (viewModel is DealerSelectDialogViewModel dealerSelect)
        {
            dealerSelect.CloseRequested += result => window.Close(result);
        }
        else if (viewModel is PriceModeDialogViewModel priceMode)
        {
            priceMode.CloseRequested += result => window.Close(result);
        }
        else if (viewModel is ConfirmDeleteDialogViewModel confirmDelete)
        {
            confirmDelete.CloseRequested += result => window.Close(result);
        }

        var owner = ResolveOwner(window);
        return owner is not null
            ? await window.ShowDialog<bool?>(owner)
            : await window.ShowDialog<bool?>(null!);
    }

    private static Window? ResolveOwner(Window dialog)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}
