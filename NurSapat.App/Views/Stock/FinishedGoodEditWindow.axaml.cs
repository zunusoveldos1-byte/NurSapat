using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Stock;

public partial class FinishedGoodEditWindow : Window
{
    public FinishedGoodEditWindow()
    {
        InitializeComponent();
    }

    public FinishedGoodEditWindow(FinishedGoodEditViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool result) => Close(result);

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);
}
