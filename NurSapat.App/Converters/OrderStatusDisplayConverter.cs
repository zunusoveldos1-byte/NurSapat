using Avalonia.Data.Converters;
using NurSapat.Core.Models.Orders;
using System.Globalization;

namespace NurSapat.App.Converters;

public sealed class OrderStatusDisplayConverter : IValueConverter
{
    public static readonly OrderStatusDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is OrderStatus status ? status.ToDisplayName() : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
