using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.Cad.UI.Helpers;

namespace NurSapat.Cad.UI.Behaviors;

public static class SignedDecimalInputBehavior
{
    public static readonly AttachedProperty<bool> AllowNegativeProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("AllowNegative", typeof(SignedDecimalInputBehavior));

    public static bool GetAllowNegative(TextBox element) => element.GetValue(AllowNegativeProperty);

    public static void SetAllowNegative(TextBox element, bool value) => element.SetValue(AllowNegativeProperty, value);

    static SignedDecimalInputBehavior()
    {
        AllowNegativeProperty.Changed.AddClassHandler<TextBox>(OnAllowNegativeChanged);
    }

    private static void OnAllowNegativeChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            textBox.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
            return;
        }

        textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
    }

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var text = textBox.Text ?? string.Empty;
        var start = Math.Clamp(textBox.SelectionStart, 0, text.Length);
        var end = Math.Clamp(textBox.SelectionEnd, start, text.Length);
        var proposed = text.Remove(start, end - start).Insert(start, e.Text ?? string.Empty);

        var isValid = GetAllowNegative(textBox)
            ? SignedDecimalInput.IsValidSignedCoordinateText(proposed)
            : SignedDecimalInput.IsValidPositiveDimensionText(proposed);

        if (!isValid)
        {
            e.Handled = true;
        }
    }
}
