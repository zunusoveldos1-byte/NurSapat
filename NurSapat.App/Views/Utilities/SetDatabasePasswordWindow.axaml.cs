using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Utilities;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Utilities;

public partial class SetDatabasePasswordWindow : Window
{
    public SetDatabasePasswordWindow()
    {
        InitializeComponent();
    }

    public SetDatabasePasswordWindow(SetDatabasePasswordViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose() => Close();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
