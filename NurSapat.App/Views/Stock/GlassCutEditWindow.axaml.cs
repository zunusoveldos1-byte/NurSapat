using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Stock;

public partial class GlassCutEditWindow : Window
{
    public GlassCutEditWindow()
    {
        InitializeComponent();
    }

    public GlassCutEditWindow(GlassCutEditViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += result => Close(result);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);
}
