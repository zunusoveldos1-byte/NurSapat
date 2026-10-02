using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dictionaries;

public partial class ColorsDictionaryWindow : Window
{
    private readonly WindowColorsDictionaryViewModel _viewModel = null!;

    public ColorsDictionaryWindow()
    {
        InitializeComponent();
    }

    public ColorsDictionaryWindow(WindowColorsDictionaryViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;

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

    private async void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.SelectedItem is not null)
        {
            await _viewModel.AssignColorCommand.ExecuteAsync(_viewModel.SelectedItem);
        }
    }

    private async Task<string?> PickColorHexAsync()
    {
        var current = _viewModel.SelectedItem?.HexCode ?? "#FFFFFF";
        var dialog = new WindowColorPickerDialog(current);
        return await dialog.ShowDialog<string?>(this);
    }
}
