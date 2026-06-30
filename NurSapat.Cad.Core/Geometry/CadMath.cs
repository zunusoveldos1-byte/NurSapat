namespace NurSapat.Cad.Core.Geometry;

public static class CadMath
{
    public const double Epsilon = 0.000001;

    /// <summary>Округление до сотых долей миллиметра (стандарт CAD).</summary>
    public const double MmPrecision = 0.01;

    public static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    public static double RoundMm(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static bool NearlyEqual(double a, double b, double tolerance = MmPrecision) =>
        Math.Abs(a - b) <= tolerance;

    public static CadPoint? Intersect(CadLine a, CadLine b)
    {
        var p = a.Start;
        var r = a.End - a.Start;
        var q = b.Start;
        var s = b.End - b.Start;
        var denominator = r.Cross(s);

        if (Math.Abs(denominator) < Epsilon)
        {
            return null;
        }

        var t = (q - p).Cross(s) / denominator;
        var u = (q - p).Cross(r) / denominator;

        if (t is < -Epsilon or > 1 + Epsilon || u is < -Epsilon or > 1 + Epsilon)
        {
            return null;
        }

        return new CadPoint(p.X + t * r.X, p.Y + t * r.Y);
    }
}
