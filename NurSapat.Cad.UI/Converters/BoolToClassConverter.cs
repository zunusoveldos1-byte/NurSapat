using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NurSapat.Cad.UI.Converters;

public class BoolToClassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is true) ? "active" : "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}