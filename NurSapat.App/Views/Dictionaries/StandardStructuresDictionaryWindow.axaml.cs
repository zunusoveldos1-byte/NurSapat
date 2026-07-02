using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dictionaries;

public partial class StandardStructuresDictionaryWindow : Window
{
    public StandardStructuresDictionaryWindow()
    {
        InitializeComponent();
    }

    public StandardStructuresDictionaryWindow(StandardStructuresDictionaryViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += () => Close();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.Minimize(this);

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.ToggleMaximize(this);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
