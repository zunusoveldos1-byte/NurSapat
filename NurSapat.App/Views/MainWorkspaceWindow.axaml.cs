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
        // Инициализация CAD при первом запуске
        if (DataContext is MainWorkspaceViewModel vm && vm.CurrentContent == null)
        {
            // ИЗМЕНЕНИЕ: используем конструктор без параметров
            var cadVm = new CadEditorViewModel();
            vm.CurrentContent = cadVm;
        }

        // ДОБАВЛЕНО: Передаём ссылку на окно в ViewModel
        if (DataContext is MainWorkspaceViewModel vm2)
        {
            vm2.SetOwnerWindow(this);
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