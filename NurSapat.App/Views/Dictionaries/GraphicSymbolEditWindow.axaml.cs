using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dictionaries;

public partial class GraphicSymbolEditWindow : Window
{
    public GraphicSymbolEditWindow()
    {
        InitializeComponent();
    }

    public GraphicSymbolEditWindow(GraphicSymbolEditViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool result) => Close(result);

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);
}
