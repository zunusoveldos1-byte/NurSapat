using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Materials;

namespace NurSapat.App.Views.Materials;

public partial class MaterialsDictionaryWindow : Window
{
    public MaterialsDictionaryWindow()
    {
        InitializeComponent();
    }

    public MaterialsDictionaryWindow(MaterialsDictionaryViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        Title = viewModel.Title;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.Minimize(this);

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.ToggleMaximize(this);

    private void OnCloseClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.Close(this);

}
