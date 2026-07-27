using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.Models.Settings;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dialogs;

public partial class ExternalUtilityEditDialog : Window
{
    public ExternalUtilityEditDialog()
    {
        InitializeComponent();
    }

    public ExternalUtilityEditDialog(ExternalUtilityEditDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(ExternalUtilityItem? result) => Close(result);

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(null);
}
