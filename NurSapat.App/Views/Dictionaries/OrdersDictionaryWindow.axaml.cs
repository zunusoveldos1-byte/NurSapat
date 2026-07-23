using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dictionaries;

public partial class OrdersDictionaryWindow : Window
{
    private readonly OrdersDictionaryViewModel _viewModel = null!;

    public OrdersDictionaryWindow()
    {
        InitializeComponent();
    }

    public OrdersDictionaryWindow(OrdersDictionaryViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.OwnerWindow = this;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose() => Close();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.Minimize(this);

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.ToggleMaximize(this);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        _viewModel.SyncSelectedOrders(grid.SelectedItems.OfType<OrderDictionaryItemViewModel>());
    }

    private async void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.SelectedOrders.Count > 0)
        {
            await _viewModel.OpenOrderCommand.ExecuteAsync(null);
        }
    }
}
