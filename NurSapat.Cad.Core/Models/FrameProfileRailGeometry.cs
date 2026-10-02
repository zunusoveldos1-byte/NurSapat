using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

/// <summary>Точная геометрия отдельных профилей прямоугольной коробки (совпадает с FrameVolumeRenderer.DrawRail).</summary>
public static class FrameProfileRailGeometry
{
    public readonly record struct RailBand(CadPoint OuterStart, CadPoint OuterEnd, CadPoint InnerEnd, CadPoint InnerStart);

    public static RailBand ResolveRailBand(
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        FrameProfileRail rail)
    {
        var half = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThickness) / 2.0;
        var x0 = originX;
        var y0 = originY;
        var x1 = originX + width;
        var y1 = originY + height;
        var xi0 = x0 + half;
        var xi1 = x1 - half;
        var yi0 = y0 + half;
        var yi1 = y1 - half;

        return rail switch
        {
            FrameProfileRail.Top => new RailBand(
                new CadPoint(x0, y1),
                new CadPoint(x1, y1),
                new CadPoint(xi1, yi1),
                new CadPoint(xi0, yi1)),
            FrameProfileRail.Bottom => new RailBand(
                new CadPoint(x0, y0),
                new CadPoint(x1, y0),
                new CadPoint(xi1, yi0),
                new CadPoint(xi0, yi0)),
            FrameProfileRail.Left => new RailBand(
                new CadPoint(x0, y0),
                new CadPoint(x0, y1),
                new CadPoint(xi0, yi1),
                new CadPoint(xi0, yi0)),
            FrameProfileRail.Right => new RailBand(
                new CadPoint(x1, y0),
                new CadPoint(x1, y1),
                new CadPoint(xi1, yi1),
                new CadPoint(xi1, yi0)),
            _ => default
        };
    }

    public static bool TryHitRectangularFrameRail(
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        CadPoint point,
        out FrameProfileRail rail)
    {
        rail = default;
        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return false;
        }

        var best = double.MaxValue;
        var found = false;
        foreach (var candidate in new[]
                 {
                     FrameProfileRail.Top,
                     FrameProfileRail.Bottom,
                     FrameProfileRail.Left,
                     FrameProfileRail.Right
                 })
        {
            var band = ResolveRailBand(originX, originY, width, height, profileThickness, candidate);
            if (!ContainsPointInBand(band, point))
            {
                continue;
            }

            var distance = DistanceToBandCenterline(band, point);
            if (distance < best)
            {
                best = distance;
                rail = candidate;
                found = true;
            }
        }

        return found;
    }

    public static bool ContainsPointInBand(RailBand band, CadPoint point) =>
        ContainsPointInPolygon(
        [
            band.OuterStart,
            band.OuterEnd,
            band.InnerEnd,
            band.InnerStart
        ],
        point);

    private static double DistanceToBandCenterline(RailBand band, CadPoint point)
    {
        var midOuter = new CadPoint(
            (band.OuterStart.X + band.OuterEnd.X) / 2.0,
            (band.OuterStart.Y + band.OuterEnd.Y) / 2.0);
        var midInner = new CadPoint(
            (band.InnerStart.X + band.InnerEnd.X) / 2.0,
            (band.InnerStart.Y + band.InnerEnd.Y) / 2.0);
        var center = new CadPoint(
            (midOuter.X + midInner.X) / 2.0,
            (midOuter.Y + midInner.Y) / 2.0);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool ContainsPointInPolygon(IReadOnlyList<CadPoint> polygon, CadPoint point)
    {
        if (polygon.Count < 3)
        {
            return false;
        }

        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];
            if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y + double.Epsilon) + pi.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
