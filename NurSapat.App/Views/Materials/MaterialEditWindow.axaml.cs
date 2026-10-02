using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Materials;

namespace NurSapat.App.Views.Materials;

public partial class MaterialEditWindow : Window
{
    private readonly MaterialEditViewModel _viewModel = null!;

    public MaterialEditWindow()
    {
        InitializeComponent();
    }

    public MaterialEditWindow(MaterialEditViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        Title = viewModel.WindowTitle;

        _viewModel.RequestClose += OnRequestClose;
        _viewModel.ShowColorDialog = ShowColorDialogAsync;
    }

    private void OnRequestClose(bool result) => Close(result);

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.Minimize(this);

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        BorderlessChromeHelper.ToggleMaximize(this);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);

    private async Task<MaterialColorPriceViewModel?> ShowColorDialogAsync()
    {
        var dialogVm = new MaterialColorAddDialogViewModel();
        var dialog = new MaterialColorAddDialog(dialogVm);
        var result = await dialog.ShowDialog<bool?>(this);
        return result == true ? dialogVm.BuildRow() : null;
    }
}
