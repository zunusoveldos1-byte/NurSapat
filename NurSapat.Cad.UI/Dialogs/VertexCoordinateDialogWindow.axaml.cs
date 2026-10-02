using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.Cad.UI.Dialogs;

public partial class VertexCoordinateDialogWindow : Window
{
    public VertexCoordinateDialogWindow()
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
