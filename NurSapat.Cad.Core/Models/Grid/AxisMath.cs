using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>
/// Единая осесимметричная математика: формулы пишутся один раз,
/// ориентация выбирает только какую пару координат использовать.
/// </summary>
public static class AxisMath
{
    /// <summary>
    /// Координата точки вдоль оси линии (X для Vertical, Y для Horizontal).
    /// </summary>
    public static double AlongCoordinate(GridLineOrientation orientation, double x, double y) =>
        orientation == GridLineOrientation.Vertical ? x : y;

    /// <summary>
    /// Координата точки вдоль пролёта перпендикулярно линии
    /// (Y для Vertical-линии, X для Horizontal-линии).
    /// </summary>
    public static double AlongSpan(GridLineOrientation orientation, double x, double y) =>
        orientation == GridLineOrientation.Vertical ? y : x;

    public static double DistanceToLine(GridLine line, double x, double y) =>
        Math.Abs(AlongCoordinate(line.Orientation, x, y) - line.Coordinate);

    /// <summary>
    /// Грани профиля вокруг оси: Face1 = center - t/2, Face2 = center + t/2.
    /// Одна формула для обеих ориентаций.
    /// </summary>
    public static (double Face1, double Face2, double Center) ResolveFaces(
        double coordinate,
        double thicknessMm)
    {
        var half = thicknessMm / 2.0;
        var face1 = coordinate - half;
        var face2 = coordinate + half;
        return (face1, face2, coordinate);
    }

    public static (double Face1, double Face2, double Center) ResolveFaces(GridLine line) =>
        ResolveFaces(line.Coordinate, line.ThicknessMm);

    /// <summary>
    /// Пролёт контейнера перпендикулярно ориентации линии (абсолютные min/max).
    /// </summary>
    public static (double SpanMin, double SpanMax) ContainerSpan(
        GridLineOrientation orientation,
        double containerX,
        double containerY,
        double containerWidth,
        double containerHeight) =>
        orientation == GridLineOrientation.Vertical
            ? (containerY, containerY + containerHeight)
            : (containerX, containerX + containerWidth);

    /// <summary>
    /// Диапазон оси контейнера (min/max вдоль Coordinate).
    /// </summary>
    public static (double AxisMin, double AxisMax) ContainerAxisRange(
        GridLineOrientation orientation,
        double containerX,
        double containerY,
        double containerWidth,
        double containerHeight) =>
        orientation == GridLineOrientation.Vertical
            ? (containerX, containerX + containerWidth)
            : (containerY, containerY + containerHeight);

    public static bool NearlyEqualCoordinate(double a, double b) =>
        Math.Abs(a - b) <= CadMath.MmPrecision;
}
