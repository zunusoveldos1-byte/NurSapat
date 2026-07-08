using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.Cad.UI.Dialogs;

public partial class SillDialogWindow : Window
{
    public SillDialogWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }
}
