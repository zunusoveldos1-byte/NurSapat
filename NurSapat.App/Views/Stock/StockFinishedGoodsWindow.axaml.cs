using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Stock;

public partial class StockFinishedGoodsWindow : Window
{
    private readonly StockFinishedGoodsViewModel _viewModel = null!;

    public StockFinishedGoodsWindow()
    {
        InitializeComponent();
    }

    public StockFinishedGoodsWindow(StockFinishedGoodsViewModel viewModel) : this()
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

    private async void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.SelectedItem is not null)
        {
            await _viewModel.OpenEditorForItemAsync(_viewModel.SelectedItem);
        }
    }
}
