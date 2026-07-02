using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels;
using NurSapat.App.Views.Dictionaries;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Settings;

public partial class EnvironmentSettingsWindow : Window
{
    private readonly EnvironmentSettingsViewModel? _viewModel;

    public EnvironmentSettingsWindow()
    {
        InitializeComponent();
    }

    public EnvironmentSettingsWindow(EnvironmentSettingsViewModel viewModel)
        : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.PickColorHexAsync = PickColorHexAsync;
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

    private void OnUtilityDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.EditUtilityCommand.CanExecute(null) == true)
        {
            _viewModel.EditUtilityCommand.Execute(null);
        }
    }

    private async Task<string?> PickColorHexAsync()
    {
        var dialog = new WindowColorPickerDialog(_viewModel!.SelectedElementsColor);
        return await dialog.ShowDialog<string?>(this);
    }
}
