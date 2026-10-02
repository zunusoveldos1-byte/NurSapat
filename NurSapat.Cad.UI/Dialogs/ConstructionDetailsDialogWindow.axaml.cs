using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.Cad.UI.Dialogs;

public partial class ConstructionDetailsDialogWindow : Window
{
    public ConstructionDetailsDialogWindow()
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
