using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NurSapat.Cad.UI.Converters;

public class LeftPanelIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Если панель открыта (true) -> показываем стрелку влево (свернуть)
        // Если панель закрыта (false) -> показываем стрелку вправо (развернуть)
        return (value as bool?) == true ? "◀" : "▶";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RightPanelIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Если панель открыта (true) -> показываем стрелку вправо (свернуть)
        // Если панель закрыта (false) -> показываем стрелку влево (развернуть)
        return (value as bool?) == true ? "▶" : "◀";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}