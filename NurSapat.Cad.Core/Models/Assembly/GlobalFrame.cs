using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Внешний габаритный контейнер конструкции. Не участвует в split/impost math.
/// </summary>
public sealed class GlobalFrame
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public double Right => X + Width;
    public double Top => Y + Height;

    public static GlobalFrame FromBounds(double x, double y, double width, double height) =>
        new()
        {
            X = CadMath.RoundMm(x),
            Y = CadMath.RoundMm(y),
            Width = CadMath.RoundMm(width),
            Height = CadMath.RoundMm(height)
        };
}
