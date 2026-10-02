using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations;

/// <summary>Расчёт суммарной длины декоративных шпросов (погонные метры).</summary>
public static class DecorativeSpokeMetricsCalculator
{
    private const double DecorativeBarDepthMm = 10.0;

    public static double CalculateTotalLengthMm(GeometryElement element)
    {
        if (!DecorativeSpokeElement.TryFromGeometryElement(element, out var spoke))
        {
            return 0;
        }

        var left = spoke.CenterX - spoke.WidthMm / 2.0;
        var right = spoke.CenterX + spoke.WidthMm / 2.0;
        var bottom = spoke.CenterY - spoke.HeightMm / 2.0;
        var top = spoke.CenterY + spoke.HeightMm / 2.0;
        var width = spoke.WidthMm;
        var height = spoke.HeightMm;

        return spoke.SpokeType switch
        {
            DecorativeSpokeType.StandardGrid => CalculateGridLength(
                left, bottom, right, top,
                Math.Max(1, spoke.RowsCount),
                Math.Max(1, spoke.ColumnsCount)),
            DecorativeSpokeType.Sunburst => CalculateSunburstLength(
                spoke, left, bottom, right, top, width, height),
            DecorativeSpokeType.ArchContour => CalculateArchContourLength(spoke),
            DecorativeSpokeType.Diamond => CalculateDiamondLength(left, bottom, right, top, width, height),
            _ => 0
        };
    }

    public static double CalculateCrossSectionAreaMm2(GeometryElement element)
    {
        if (!DecorativeSpokeElement.TryFromGeometryElement(element, out var spoke))
        {
            return 0;
        }

        var thickness = spoke.Thickness > 0 ? spoke.Thickness : 18;
        return thickness * DecorativeBarDepthMm;
    }

    private static double CalculateGridLength(
        double left,
        double bottom,
        double right,
        double top,
        int rows,
        int columns)
    {
        var width = right - left;
        var height = top - bottom;
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        var verticalLines = Math.Max(0, columns - 1);
        var horizontalLines = Math.Max(0, rows - 1);
        return verticalLines * height + horizontalLines * width;
    }

    private static double CalculateSunburstLength(
        DecorativeSpokeElement spoke,
        double left,
        double bottom,
        double right,
        double top,
        double width,
        double height)
    {
        var rayCount = Math.Max(2, spoke.RayCount);
        if (spoke.IsArched && spoke.ArcRadius > 0)
        {
            return rayCount * spoke.ArcRadius;
        }

        var centerX = (left + right) / 2.0;
        var centerY = bottom;
        var maxRadius = Math.Max(
            Math.Sqrt(Math.Pow(left - centerX, 2) + Math.Pow(top - centerY, 2)),
            Math.Sqrt(Math.Pow(right - centerX, 2) + Math.Pow(top - centerY, 2)));
        return rayCount * maxRadius;
    }

    private static double CalculateArchContourLength(DecorativeSpokeElement spoke)
    {
        if (spoke.IsArched && spoke.ArcRadius > 0)
        {
            var inset = Math.Min(spoke.ArcRadius * 0.25, 80);
            var innerRadius = Math.Max(spoke.ArcRadius - inset, 1);
            return Math.PI * innerRadius;
        }

        var width = spoke.WidthMm;
        return width > 0 ? Math.PI * width / 4.0 : 0;
    }

    private static double CalculateDiamondLength(
        double left,
        double bottom,
        double right,
        double top,
        double width,
        double height)
    {
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        var diagonal1 = Math.Sqrt(width * width + height * height);
        var midTopX = (left + right) / 2.0;
        var midBottomX = midTopX;
        var midLeftY = (bottom + top) / 2.0;
        var midRightY = midLeftY;
        var rhombusSide = Math.Sqrt(
            Math.Pow(midTopX - left, 2) + Math.Pow(top - midLeftY, 2)) * 4;
        return diagonal1 * 2 + rhombusSide;
    }
}
