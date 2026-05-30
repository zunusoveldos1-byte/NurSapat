namespace NurSapat.Cad.Core.Geometry;

public readonly record struct CadVector(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);

    public CadVector Normalized()
    {
        var length = Length;
        return length <= CadMath.Epsilon ? new CadVector(0, 0) : new CadVector(X / length, Y / length);
    }

    public double Dot(CadVector other) => X * other.X + Y * other.Y;

    public double Cross(CadVector other) => X * other.Y - Y * other.X;

    public CadVector Perpendicular() => new(-Y, X);

    public static CadVector operator +(CadVector left, CadVector right) => new(left.X + right.X, left.Y + right.Y);

    public static CadVector operator -(CadVector left, CadVector right) => new(left.X - right.X, left.Y - right.Y);

    public static CadVector operator *(CadVector vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);

    public static CadVector operator /(CadVector vector, double scalar) => new(vector.X / scalar, vector.Y / scalar);
}
