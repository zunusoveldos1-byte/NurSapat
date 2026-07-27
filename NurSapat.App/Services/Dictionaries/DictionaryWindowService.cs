using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.ViewModels;
using NurSapat.App.ViewModels.Config;
using NurSapat.App.ViewModels.Settings;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.ViewModels.Materials;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Dictionaries;
using NurSapat.App.Views.Materials;
using NurSapat.App.Views.Stock;
using NurSapat.App.Views.Settings;
using NurSapat.App.Views.Config;

namespace NurSapat.App.Services.Dictionaries;

public interface IDictionaryWindowService
{
    Task ShowMaterialsAsync(Window? owner);

    Task ShowStockProfileCutsAsync(Window? owner);

    Task ShowStockGlassCutsAsync(Window? owner);

    Task ShowGraphicSymbolsAsync(Window? owner);

    Task ShowStandardStructuresAsync(Window? owner);

    Task ShowUsersAsync(Window? owner);

    Task ShowProfileSeriesAsync(Window? owner);

    Task ShowCustomersAsync(Window? owner);

    Task<CustomerItem?> ShowCustomersForSelectionAsync(Window? owner);

    Task ShowManagersAsync(Window? owner);

    Task ShowDealersPlaceholderAsync(Window? owner);

    Task ShowOrdersAsync(Window? owner);

    Task ShowStockMaterialsAsync(Window? owner);

    Task ShowExchangeRatesAsync(Window? owner);

    Task ShowManufacturersAsync(Window? owner);

    Task ShowStockFinishedGoodsAsync(Window? owner);

    Task ShowSuppliersAsync(Window? owner);

    Task ShowOrderTemplatesAsync(Window? owner);

    Task ShowStructureNamesAsync(Window? owner);

    Task ShowWindowColorsAsync(Window? owner);

    Task ShowEnvironmentSettingsAsync(Window? owner);

    Task ShowConstructionElementsAsync(Window? owner);

    Task ShowChangeConfigurationAsync(Window? owner);
}

public sealed class DictionaryWindowService(IServiceProvider serviceProvider) : IDictionaryWindowService
{
    public async Task ShowMaterialsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<MaterialsDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new MaterialsDictionaryWindow(viewModel), owner);
    }

    public async Task ShowStockProfileCutsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<StockProfileCutsDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new StockProfileCutsDictionaryWindow(viewModel), owner);
    }

    public async Task ShowStockGlassCutsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<StockGlassCutsDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new StockGlassCutsDictionaryWindow(viewModel), owner);
    }

    public async Task ShowGraphicSymbolsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<GraphicSymbolsDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new GraphicSymbolsDictionaryWindow(viewModel), owner);
    }

    public async Task ShowStandardStructuresAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<StandardStructuresDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new StandardStructuresDictionaryWindow(viewModel), owner);
    }

    public async Task ShowUsersAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<UsersDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new UsersDictionaryWindow(viewModel), owner);
    }

    public async Task ShowProfileSeriesAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<SeriesDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new SeriesDictionaryWindow(viewModel), owner);
    }

    public async Task ShowCustomersAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<CustomersDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new CustomersDictionaryWindow(viewModel), owner);
    }

    public async Task<CustomerItem?> ShowCustomersForSelectionAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<CustomersDictionaryViewModel>();
        viewModel.ConfigureForOrderSelection();
        await viewModel.InitializeAsync();

        var window = new CustomersDictionaryWindow(viewModel);
        viewModel.OwnerWindow = window;
        await ShowDialogSafeAsync(window, owner);
        return viewModel.SelectedCustomerResult;
    }

    public async Task ShowManagersAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ManagersDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ManagersDictionaryWindow(viewModel), owner);
    }

    public async Task ShowDealersPlaceholderAsync(Window? owner) =>
        await ShowDialogSafeAsync(new DealersPlaceholderWindow(), owner);

    public async Task ShowOrdersAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<OrdersDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new OrdersDictionaryWindow(viewModel), owner);
    }

    public async Task ShowStockMaterialsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<StockMaterialsViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new StockMaterialsWindow(viewModel), owner);
    }

    public async Task ShowExchangeRatesAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ExchangeRatesDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ExchangeRatesDictionaryWindow(viewModel), owner);
    }

    public async Task ShowManufacturersAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ManufacturersDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ManufacturersDictionaryWindow(viewModel), owner);
    }

    public async Task ShowStockFinishedGoodsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<StockFinishedGoodsViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new StockFinishedGoodsWindow(viewModel), owner);
    }

    public async Task ShowSuppliersAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<SuppliersDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new SuppliersDictionaryWindow(viewModel), owner);
    }

    public async Task ShowOrderTemplatesAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<OrderTemplatesDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new OrderTemplatesDictionaryWindow(viewModel), owner);
    }

    public async Task ShowStructureNamesAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<StructureNamesDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new StructureNamesDictionaryWindow(viewModel), owner);
    }

    public async Task ShowWindowColorsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<WindowColorsDictionaryViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ColorsDictionaryWindow(viewModel), owner);
    }

    public async Task ShowEnvironmentSettingsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<EnvironmentSettingsViewModel>();
        await ShowDialogSafeAsync(new EnvironmentSettingsWindow(viewModel), owner);
    }

    public async Task ShowConstructionElementsAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ConstructionElementsViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ConstructionElementsWindow(viewModel), owner);
    }

    public async Task ShowChangeConfigurationAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ChangeConfigurationViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ChangeConfigurationWindow(viewModel), owner);
    }

    private static async Task ShowDialogSafeAsync(Window window, Window? owner)
    {
        var resolvedOwner = owner ?? ResolveMainWindow();

        if (resolvedOwner is not null)
        {
            await window.ShowDialog(resolvedOwner);
            return;
        }

        window.Show();
    }

    private static Window? ResolveMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}
