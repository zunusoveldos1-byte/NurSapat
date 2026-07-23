using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dialogs;

public partial class CreateLocalAccountDialog : Window
{
    public CreateLocalAccountDialog()
    {
        InitializeComponent();
    }

    public CreateLocalAccountDialog(CreateLocalAccountDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose((string Email, string Password)? result) => Close(result);

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(null);
}
