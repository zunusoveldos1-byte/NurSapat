using Avalonia.Controls;
using Avalonia.Input; // пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ using пїЅпїЅпїЅ PointerPressedEventArgs

namespace NurSapat.App.Views.Dialogs;

public partial class PrintSettingsWindow : Window
{
    public PrintSettingsWindow()
    {
        InitializeComponent();
    }

    // пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}