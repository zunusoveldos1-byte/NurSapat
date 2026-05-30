namespace NurSapat.Cad.Core.Geometry;

public readonly record struct CadPoint(double X, double Y)
{
    public static CadPoint Origin { get; } = new(0, 0);

    public double DistanceTo(CadPoint other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public CadPoint Round(int digits = 6) => new(Math.Round(X, digits), Math.Round(Y, digits));

    public static CadPoint operator +(CadPoint point, CadVector vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    public static CadPoint operator -(CadPoint point, CadVector vector) =>
        new(point.X - vector.X, point.Y - vector.Y);

    public static CadVector operator -(CadPoint left, CadPoint right) =>
        new(left.X - right.X, left.Y - right.Y);
}
