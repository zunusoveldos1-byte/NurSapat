using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Materials;

namespace NurSapat.App.Views.Materials;

public partial class MaterialColorAddDialog : Window
{
    public MaterialColorAddDialog()
    {
        InitializeComponent();
    }

    public MaterialColorAddDialog(MaterialColorAddDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += result => Close(result);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);
}
