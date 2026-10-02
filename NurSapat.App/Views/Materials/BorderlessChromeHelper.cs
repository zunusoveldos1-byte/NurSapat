using Avalonia.Controls;
using Avalonia.Input;

namespace NurSapat.App.Views.Materials;

internal static class BorderlessChromeHelper
{
    public static void OnTitleBarPointerPressed(Window window, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            window.BeginMoveDrag(e);
        }
    }

    public static void Minimize(Window window) => window.WindowState = WindowState.Minimized;

    public static void ToggleMaximize(Window window) =>
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    public static void Close(Window window) => window.Close();
}
