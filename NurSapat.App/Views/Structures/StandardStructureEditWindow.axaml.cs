using Avalonia.Controls;
using Avalonia.Input;
using NurSapat.App.ViewModels.Structures;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Structures;

public partial class StandardStructureEditWindow : Window
{
    public StandardStructureEditWindow()
    {
        InitializeComponent();
    }

    public StandardStructureEditWindow(StandardStructureEditViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += result => Close(result);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);
}
