using Avalonia.Controls;
using Avalonia.Input; // ОБЯЗАТЕЛЬНО добавьте этот using для PointerPressedEventArgs

namespace NurSapat.Cad.UI.Dialogs;

public partial class ImpostCountWindow : Window
{
    public ImpostCountWindow()
    {
        InitializeComponent();
    }

    // Метод для перетаскивания окна (привязывается к PointerPressed в XAML)
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}