using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;

namespace NurSapat.App.Views.Dictionaries;

public partial class WindowColorPickerDialog : Window
{
    private readonly ColorPicker _picker = null!;

    public WindowColorPickerDialog()
    {
        InitializeComponent();
    }

    public WindowColorPickerDialog(string currentHex) : this()
    {

        _picker = this.FindControl<ColorPicker>("ColorPickerControl")
            ?? throw new InvalidOperationException("ColorPickerControl not found.");

        _picker.IsAlphaEnabled = false;
        _picker.Color = TryParseColor(currentHex);
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e) => Close(ToHex(_picker.Color));

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);

    private static Color TryParseColor(string hex) =>
        Color.TryParse(hex, out var color) ? color : Colors.White;

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
