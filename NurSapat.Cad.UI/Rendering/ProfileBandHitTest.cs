using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Rendering;

internal static class ProfileBandHitTest
{
    public static bool ContainsPoint(ProfileContourLoop outerLoop, ProfileContourLoop innerLoop, CadPoint point)
    {
        var outer = outerLoop.Tessellate();
        var inner = innerLoop.Tessellate();
        return ContainsPointInPolygon(outer, point) && !ContainsPointInPolygon(inner, point);
    }

    public static bool IntersectsRect(ProfileContourLoop outerLoop, ProfileContourLoop innerLoop, CadRect rect)
    {
        if (RectIntersectsPolygon(outerLoop.Tessellate(), rect))
        {
            return true;
        }

        return ContainsPoint(outerLoop, innerLoop, rectCenter(rect));
    }

    public static bool ContainsPointInRectangularBand(
        double outerLeft,
        double outerBottom,
        double outerRight,
        double outerTop,
        double innerLeft,
        double innerBottom,
        double innerRight,
        double innerTop,
        CadPoint point)
    {
        if (point.X < outerLeft || point.X > outerRight || point.Y < outerBottom || point.Y > outerTop)
        {
            return false;
        }

        return point.X < innerLeft || point.X > innerRight || point.Y < innerBottom || point.Y > innerTop;
    }

    public static bool RectIntersectsRectangularBand(
        CadRect selection,
        double outerLeft,
        double outerBottom,
        double outerRight,
        double outerTop,
        double innerLeft,
        double innerBottom,
        double innerRight,
        double innerTop)
    {
        if (!selection.Intersects(new CadRect(outerLeft, outerTop, outerRight - outerLeft, outerTop - outerBottom)))
        {
            return false;
        }

        var center = rectCenter(selection);
        if (ContainsPointInRectangularBand(
                outerLeft, outerBottom, outerRight, outerTop,
                innerLeft, innerBottom, innerRight, innerTop,
                center))
        {
            return true;
        }

        return selection.Intersects(new CadRect(innerLeft, innerTop, innerRight - innerLeft, innerTop - innerBottom));
    }

    public static bool ContainsPointInPolygon(IReadOnlyList<CadPoint> polygon, CadPoint point)
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

    public static bool RectIntersectsPolygon(IReadOnlyList<CadPoint> polygon, CadRect rect)
    {
        if (polygon.Count < 3)
        {
            return false;
        }

        var corners = new[]
        {
            new CadPoint(rect.X, rect.Y),
            new CadPoint(rect.X + rect.Width, rect.Y),
            new CadPoint(rect.X + rect.Width, rect.Y - rect.Height),
            new CadPoint(rect.X, rect.Y - rect.Height)
        };

        foreach (var corner in corners)
        {
            if (ContainsPointInPolygon(polygon, corner))
            {
                return true;
            }
        }

        var center = rectCenter(rect);
        if (ContainsPointInPolygon(polygon, center))
        {
            return true;
        }

        foreach (var vertex in polygon)
        {
            if (rect.Contains(vertex))
            {
                return true;
            }
        }

        return false;
    }

    private static CadPoint rectCenter(CadRect rect) =>
        new(rect.X + rect.Width / 2.0, rect.Y - rect.Height / 2.0);
}
