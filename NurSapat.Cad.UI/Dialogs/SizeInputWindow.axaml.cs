using Avalonia.Controls;
using Avalonia.Input;          // для FocusChangedEventArgs
using Avalonia.Threading;     // для Dispatcher

namespace NurSapat.Cad.UI.Dialogs;

public partial class SizeInputWindow : Window
{
    public SizeInputWindow()
    {
        InitializeComponent();
    }

    // Перетаскивание окна за пустые области
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    // Клик по метке "Ширина" – переключаем фокус на поле
    private void OnWidthLabelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        WidthBox.Focus();
    }

    // Клик по метке "Высота" – переключаем фокус на поле
    private void OnHeightLabelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        HeightBox.Focus();
    }

    // Когда поле получает фокус – выделяем весь текст
    private void OnTextBoxGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Используем Dispatcher с приоритетом Background для гарантии
            Dispatcher.UIThread.InvokeAsync(
                () => textBox.SelectAll(),
                DispatcherPriority.Background);
        }
    }
}