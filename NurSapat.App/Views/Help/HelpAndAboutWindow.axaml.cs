using Avalonia.Controls;
using Avalonia.Input;
using NurSapat.App.ViewModels.Help;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Help;

public partial class HelpAndAboutWindow : Window
{
    public HelpAndAboutWindow(HelpAndAboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += Close;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);
}
