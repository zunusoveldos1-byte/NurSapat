using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dialogs;

public partial class CreateProfileDialog : Window
{
    public CreateProfileDialog()
    {
        InitializeComponent();
    }

    public CreateProfileDialog(CreateProfileDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(string? result) => Close(result);

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(null);
}
