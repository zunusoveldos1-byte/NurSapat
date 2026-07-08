using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Export;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Export;

public partial class ExportXlsWindow : Window
{
    public ExportXlsWindow(ExportXlsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose() => Close();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
