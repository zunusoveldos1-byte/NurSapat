using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.App.Views.Dialogs.Edit;

public partial class CopyDrawingDialogWindow : Window
{
    public CopyDrawingDialogWindow()
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
