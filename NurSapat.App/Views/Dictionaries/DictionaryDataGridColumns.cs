using Avalonia.Controls;
using Avalonia.Data;

namespace NurSapat.App.Views.Dictionaries;

internal static class DictionaryDataGridColumns
{
    public static DataGridTextColumn Text(string header, string bindingPath, string? width = null, bool readOnly = true) =>
        new()
        {
            Header = header,
            Binding = new Binding(bindingPath),
            IsReadOnly = readOnly,
            Width = ParseWidth(width)
        };

    private static DataGridLength ParseWidth(string? width) =>
        width switch
        {
            "*" => new DataGridLength(1, DataGridLengthUnitType.Star),
            null => DataGridLength.Auto,
            _ when width.EndsWith('*') => new DataGridLength(double.Parse(width.TrimEnd('*')), DataGridLengthUnitType.Star),
            _ => new DataGridLength(double.Parse(width), DataGridLengthUnitType.Pixel)
        };
}
