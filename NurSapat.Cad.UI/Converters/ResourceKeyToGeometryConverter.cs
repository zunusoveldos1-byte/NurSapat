using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NurSapat.Cad.UI.Converters;

/// <summary>
/// Resolves a string resource key (e.g. "Icon.CorrugationVertical") to a
/// <see cref="Geometry"/> from the application-wide resource dictionary.
/// </summary>
public sealed class ResourceKeyToGeometryConverter : IValueConverter
{
    public static readonly ResourceKeyToGeometryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrEmpty(key))
            return null;

        if (Application.Current?.Resources.TryGetResource(key, null, out var resource) != true)
            return null;

        return resource switch
        {
            Geometry geometry => geometry,
            IGeometrySource source => source.ToGeometry(),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
