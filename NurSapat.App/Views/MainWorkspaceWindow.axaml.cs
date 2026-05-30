using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Cad.UI.Views;

namespace NurSapat.App.Views;

public partial class MainWorkspaceWindow : Window
{
    public MainWorkspaceWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Если DataContext - это MainWorkspaceViewModel, инициализируем CurrentContent
        if (DataContext is MainWorkspaceViewModel vm && vm.CurrentContent == null)
        {
            // Создаем ViewModel для CAD-редактора (потребуется OrderCalculationState)
            var orderState = new OrderCalculationState();
            var cadVm = new CadEditorViewModel(orderState);
            vm.CurrentContent = cadVm;
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Здесь можно сохранить ширину панелей
    }

    private (double, double) GetCurrentPanelWidths()
    {
        var editor = this.FindDescendantOfType<CadEditorView>();
        if (editor != null)
        {
            return (editor.GetLeftPanelWidth(), editor.GetRightPanelWidth());
        }
        return (220, 320);
    }
}