using Avalonia.Controls;
using Avalonia.Input; // Обязательно добавьте этот using

namespace NurSapat.Cad.UI.Dialogs;

public partial class SizeInputWindow : Window
{
    public SizeInputWindow()
    {
        InitializeComponent();
    }

    // Метод для перетаскивания окна мышкой
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}