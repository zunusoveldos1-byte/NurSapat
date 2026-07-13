using Avalonia.Controls;
using Avalonia.Input;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dictionaries;

public partial class OrderTemplateParametersWindow : Window
{
    public OrderTemplateParametersWindow()
    {
        InitializeComponent();
    }

    public OrderTemplateParametersWindow(OrderTemplateParametersViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += result => Close(result);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);
}
