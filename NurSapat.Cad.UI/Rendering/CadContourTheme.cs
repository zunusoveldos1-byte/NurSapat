using Avalonia.Media;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Доступные варианты глобального цвета контура чертежа.</summary>
public enum CadContourColorOption
{
    Gray,
    Black
}

/// <summary>
/// Единый централизованный источник цвета контура для всех отрисовщиков CAD-холста
/// (коробка, импосты, створки, штапик, размерные линии и прочие вспомогательные фигуры).
/// <see cref="Brush"/> — один и тот же мутируемый экземпляр <see cref="SolidColorBrush"/>,
/// на который ссылаются все Pen/Brush поля рендереров. Смена <see cref="Apply"/> меняет
/// его Color атомарно и мгновенно применяется ко всей уже построенной геометрии без
/// пересоздания кистей/перьев — достаточно перерисовать сцену.
/// </summary>
public static class CadContourTheme
{
    public static readonly Color GrayColor = Color.Parse("#919191");
    public static readonly Color BlackColor = Colors.Black;

    private static readonly SolidColorBrush ContourBrush = new(GrayColor);

    /// <summary>Общая кисть контура — используется во всех рендерерах фигур чертежа.</summary>
    public static IBrush Brush => ContourBrush;

    public static CadContourColorOption CurrentOption { get; private set; } = CadContourColorOption.Gray;

    public static void Apply(CadContourColorOption option)
    {
        CurrentOption = option;
        ContourBrush.Color = ResolveColor(option);
    }

    public static Color ResolveColor(CadContourColorOption option) =>
        option switch
        {
            CadContourColorOption.Black => BlackColor,
            _ => GrayColor
        };
}
