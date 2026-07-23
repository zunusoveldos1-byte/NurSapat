using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.Cad.UI.Dialogs;

public partial class RadiusInputWindow : Window
{
    public RadiusInputWindow()
    {
        InitializeComponent();
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
