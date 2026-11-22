using Avalonia.Media;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Доступные варианты глобального цвета контура чертежа.</summary>
public enum CadContourColorOption
{
    Gray,
    Black,
    White
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
    public static readonly Color WhiteColor = Colors.White;
    public static readonly Color SelectionColor = Color.Parse("#FF0000");

    private static readonly SolidColorBrush BaseStrokeBrush = new(GrayColor);
    private static readonly SolidColorBrush SelectedStrokeBrush = new(SelectionColor);

    /// <summary>Общая кисть контура — используется во всех рендерерах фигур чертежа.</summary>
    public static IBrush Brush => BaseStrokeBrush;

    public static IBrush SelectionBrush => SelectedStrokeBrush;

    public static Color BaseStrokeColor => BaseStrokeBrush.Color;

    public static CadContourColorOption CurrentOption { get; private set; } = CadContourColorOption.Gray;

    public static void Apply(CadContourColorOption option)
    {
        CurrentOption = option;
        BaseStrokeBrush.Color = ResolveColor(option);
    }

    public static Color ResolveColor(CadContourColorOption option) =>
        option switch
        {
            CadContourColorOption.Black => BlackColor,
            CadContourColorOption.White => WhiteColor,
            _ => GrayColor
        };
}
