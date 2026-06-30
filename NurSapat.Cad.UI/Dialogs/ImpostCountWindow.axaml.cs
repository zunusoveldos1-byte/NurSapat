using Avalonia.Controls;
using Avalonia.Input; // ќЅя«ј“≈Ћ№Ќќ добавьте этот using дл€ PointerPressedEventArgs

namespace NurSapat.Cad.UI.Dialogs;

public partial class ImpostCountWindow : Window
{
    public ImpostCountWindow()
    {
        InitializeComponent();
    }

    // ћетод дл€ перетаскивани€ окна (прив€зываетс€ к PointerPressed в XAML)
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}