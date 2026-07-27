using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.App.Behaviors;

public static class DoubleTappedCommandBehavior
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<Control, ICommand?>(
            "Command",
            typeof(DoubleTappedCommandBehavior));

    static DoubleTappedCommandBehavior()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    public static void SetCommand(Control element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    public static ICommand? GetCommand(Control element) =>
        element.GetValue(CommandProperty);

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.OldValue is not null)
        {
            control.DoubleTapped -= OnDoubleTapped;
        }

        if (args.NewValue is not null)
        {
            control.DoubleTapped += OnDoubleTapped;
        }
    }

    private static void OnDoubleTapped(object? sender, TappedEventArgs args)
    {
        if (sender is not Control control)
        {
            return;
        }

        var command = GetCommand(control);
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
            args.Handled = true;
        }
    }
}
