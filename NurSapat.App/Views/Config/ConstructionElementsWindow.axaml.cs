using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Config;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Config;

public partial class ConstructionElementsWindow : Window
{
    public ConstructionElementsWindow()
    {
        InitializeComponent();
    }

    public ConstructionElementsWindow(ConstructionElementsViewModel viewModel)
        : this()
    {
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
}
