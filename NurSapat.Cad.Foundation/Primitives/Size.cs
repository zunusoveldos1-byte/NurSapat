namespace NurSapat.Cad.Foundation.Primitives;

/// <summary>Размер прямоугольной области (ширина × высота) в пикселях или единицах.</summary>
public readonly record struct Size(double Width, double Height)
{
    public static Size Zero { get; } = new(0, 0);

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static Size operator *(Size size, double scalar) => new(size.Width * scalar, size.Height * scalar);
    public static Size operator /(Size size, double scalar) => new(size.Width / scalar, size.Height / scalar);
}
