namespace NurSapat.Cad.Core.Geometry;

public readonly record struct FixedVector(int XUnits, int YUnits)
{
    public FixedPoint X => new(XUnits);

    public FixedPoint Y => new(YUnits);

    public static FixedVector Zero { get; } = new(0, 0);

    public static FixedVector FromMillimeters(double x, double y) =>
        new(FixedPoint.FromMillimeters(x).Units, FixedPoint.FromMillimeters(y).Units);

    public static FixedVector operator +(FixedVector left, FixedVector right) =>
        new(checked(left.XUnits + right.XUnits), checked(left.YUnits + right.YUnits));

    public static FixedVector operator -(FixedVector left, FixedVector right) =>
        new(checked(left.XUnits - right.XUnits), checked(left.YUnits - right.YUnits));

    public static FixedVector operator *(FixedVector vector, int scalar) =>
        new(checked(vector.XUnits * scalar), checked(vector.YUnits * scalar));

    public static FixedVector operator /(FixedVector vector, int divisor) =>
        new(vector.XUnits / divisor, vector.YUnits / divisor);
}
