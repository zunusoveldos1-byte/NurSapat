using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
        PropertyChanged += OnWindowPropertyChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        UpdateMaximizeIcon();

        if (DataContext is MainWorkspaceViewModel vm2)
        {
            vm2.SetOwnerWindow(this);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            UpdateMaximizeIcon();
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            BeginMoveDrag(e);
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        UpdateMaximizeIcon();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateMaximizeIcon()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        MaximizeIcon.IsVisible = !isMaximized;
        RestoreIcon.IsVisible = isMaximized;
        ToolTip.SetTip(MaximizeButton, isMaximized ? "\u0412\u043e\u0441\u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u044c" : "\u0420\u0430\u0437\u0432\u0435\u0440\u043d\u0443\u0442\u044c");
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
