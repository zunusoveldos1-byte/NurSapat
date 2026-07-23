using System.Globalization;

namespace NurSapat.Cad.Core.Geometry;

public readonly record struct FixedPoint(int Units) : IComparable<FixedPoint>, IFormattable
{
    public const int UnitsPerMillimeter = 10;

    public static FixedPoint Zero { get; } = new(0);

    public double Millimeters => Units / (double)UnitsPerMillimeter;

    public static FixedPoint FromMillimeters(double millimeters)
    {
        if (double.IsNaN(millimeters) || double.IsInfinity(millimeters))
        {
            throw new ArgumentOutOfRangeException(nameof(millimeters), "Coordinate must be a finite number.");
        }

        return new FixedPoint(checked((int)Math.Round(millimeters * UnitsPerMillimeter, MidpointRounding.AwayFromZero)));
    }

    public int CompareTo(FixedPoint other) => Units.CompareTo(other.Units);

    public override string ToString() => Millimeters.ToString("0.###", CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Millimeters.ToString(format, formatProvider);

    public static implicit operator double(FixedPoint point) => point.Millimeters;

    public static explicit operator int(FixedPoint point) => point.Units;

    public static FixedPoint operator +(FixedPoint left, FixedPoint right) => new(checked(left.Units + right.Units));

    public static FixedPoint operator -(FixedPoint left, FixedPoint right) => new(checked(left.Units - right.Units));

    public static FixedPoint operator -(FixedPoint point) => new(checked(-point.Units));

    public static FixedPoint operator +(FixedPoint point, int units) => new(checked(point.Units + units));

    public static FixedPoint operator -(FixedPoint point, int units) => new(checked(point.Units - units));

    public static FixedPoint operator *(FixedPoint point, int scalar) => new(checked(point.Units * scalar));

    public static FixedPoint operator /(FixedPoint point, int divisor) => new(point.Units / divisor);

    public static bool operator <(FixedPoint left, FixedPoint right) => left.Units < right.Units;

    public static bool operator >(FixedPoint left, FixedPoint right) => left.Units > right.Units;

    public static bool operator <=(FixedPoint left, FixedPoint right) => left.Units <= right.Units;

    public static bool operator >=(FixedPoint left, FixedPoint right) => left.Units >= right.Units;
}
