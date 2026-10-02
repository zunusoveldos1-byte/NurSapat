namespace NurSapat.Cad.Core.Geometry;

public readonly record struct FixedRect(FixedPoint X, FixedPoint Y, FixedPoint Width, FixedPoint Height)
{
    public FixedPoint Left => X;

    public FixedPoint Right => X + Width;

    public FixedPoint Top => Y;

    public FixedPoint Bottom => Y - Height;

    public FixedPoint CenterX => X + Width / 2;

    public FixedPoint CenterY => Y - Height / 2;

    public bool IsEmpty => Width.Units <= 0 || Height.Units <= 0;

    public FixedRect(int x, int y, int width, int height)
        : this(new FixedPoint(x), new FixedPoint(y), new FixedPoint(width), new FixedPoint(height))
    {
    }

    public static FixedRect FromMillimeters(double x, double y, double width, double height) =>
        new(
            FixedPoint.FromMillimeters(x),
            FixedPoint.FromMillimeters(y),
            FixedPoint.FromMillimeters(width),
            FixedPoint.FromMillimeters(height));

    public void EnsureValid(string paramName)
    {
        if (Width.Units <= 0 || Height.Units <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rectangle width and height must be positive.");
        }
    }

    public bool Contains(FixedPoint x, FixedPoint y) =>
        x >= Left && x <= Right && y <= Top && y >= Bottom;

    public bool Contains(FixedPoint x, FixedPoint y, int marginUnits)
    {
        var margin = new FixedPoint(marginUnits);
        return x >= Left + margin &&
               x <= Right - margin &&
               y <= Top - margin &&
               y >= Bottom + margin;
    }

    public CadRect ToCadRect() => new(X.Millimeters, Y.Millimeters, Width.Millimeters, Height.Millimeters);

    public static implicit operator CadRect(FixedRect rect) => rect.ToCadRect();

    public static FixedRect FromCadRect(CadRect rect) =>
        FromMillimeters(rect.X, rect.Y, rect.Width, rect.Height);
}
