using Avalonia.Data.Converters;
using NurSapat.Core.Models.Orders;
using System.Globalization;

namespace NurSapat.Cad.UI.Converters;

/// <summary>Человекочитаемые подписи для списков диалога декоративных шпросов.</summary>
public sealed class DecorativeSpokeLabelConverter : IValueConverter
{
    public static readonly DecorativeSpokeLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            DecorativeSpokeType.StandardGrid => "Прямая сетка",
            DecorativeSpokeType.Sunburst => "Солнечные лучи",
            DecorativeSpokeType.ArchContour => "Дуга внутри дуги",
            DecorativeSpokeType.Diamond => "Ромбовидная",
            double thickness => $"{thickness:0} мм",
            DecorativeSpokeElement.Colors.Gold => "Золото",
            DecorativeSpokeElement.Colors.White => "Белый",
            DecorativeSpokeElement.Colors.Brown => "Коричневый",
            _ => value?.ToString() ?? string.Empty
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
