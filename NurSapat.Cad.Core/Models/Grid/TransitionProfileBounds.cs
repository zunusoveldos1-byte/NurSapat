namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>
/// Границы переходного профиля, вычисленные вокруг GridLine одной формулой для X и Y.
/// </summary>
public readonly record struct TransitionProfileBounds(
    GridLineOrientation Orientation,
    double Center,
    double Face1,
    double Face2,
    double Thickness,
    double SpanMin,
    double SpanMax)
{
    public double OriginAlongAxis => Math.Min(Face1, Face2);

    public double SizeAlongAxis => Math.Abs(Face2 - Face1);

    public double OriginAlongSpan => SpanMin;

    public double SizeAlongSpan => SpanMax - SpanMin;
}
