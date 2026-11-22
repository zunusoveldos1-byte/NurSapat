using Avalonia.Data.Converters;
using NurSapat.Core.Models.Orders;
using System.Globalization;

namespace NurSapat.App.Converters;

public sealed class PriceModeDisplayConverter : IValueConverter
{
    public static readonly PriceModeDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PriceModeType mode ? mode.ToDisplayName() : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
