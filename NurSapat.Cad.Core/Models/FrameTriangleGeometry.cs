using NurSapat.Cad.Core.Geometry;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.Core.Models;

/// <summary>Геометрия треугольной коробки и hit-test отдельных профилей прямоугольной рамы.</summary>
public static class FrameTriangleGeometry
{
    public static (CadPoint Bl, CadPoint Br, CadPoint Top) BuildOuterTriangle(
        double originX,
        double originY,
        double width,
        double height)
    {
        var bl = new CadPoint(originX, originY);
        var br = new CadPoint(originX + width, originY);
        var top = new CadPoint(originX + width / 2.0, originY + height);
        return (bl, br, top);
    }

    public static (CadPoint Bl, CadPoint Br, CadPoint Top) InsetTriangle(
        (CadPoint Bl, CadPoint Br, CadPoint Top) outer,
        double inset)
    {
        if (inset <= CadMath.Epsilon)
        {
            return outer;
        }

        var centroid = new CadPoint(
            (outer.Bl.X + outer.Br.X + outer.Top.X) / 3.0,
            (outer.Bl.Y + outer.Br.Y + outer.Top.Y) / 3.0);

        return (
            InsetToward(outer.Bl, centroid, inset),
            InsetToward(outer.Br, centroid, inset),
            InsetToward(outer.Top, centroid, inset));
    }

    public static bool IsRectangularFrameTransformableToTriangle(FrameNode root)
    {
        if (root.IsDummy || root.IsFictitiousContainer)
        {
            return false;
        }

        if (root.Shape != ConstructionShape.Rectangle)
        {
            return false;
        }

        if (!root.IsLeaf)
        {
            return false;
        }

        if (root.TopProfile?.SegmentType == SegmentType.Arc
            && root.TopProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return false;
        }

        return root.Width > CadMath.Epsilon && root.Height > CadMath.Epsilon;
    }

    private static CadPoint InsetToward(CadPoint point, CadPoint target, double distance)
    {
        var dx = target.X - point.X;
        var dy = target.Y - point.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= CadMath.Epsilon)
        {
            return point;
        }

        var scale = Math.Min(distance / length, 0.45);
        return new CadPoint(point.X + dx * scale, point.Y + dy * scale);
    }

    public static double InterpolateTopAtX(CadPoint bl, CadPoint br, CadPoint top, double worldX)
    {
        if (worldX <= top.X + CadMath.Epsilon)
        {
            var span = top.X - bl.X;
            if (span <= CadMath.Epsilon)
            {
                return top.Y;
            }

            var t = (worldX - bl.X) / span;
            return bl.Y + t * (top.Y - bl.Y);
        }

        var rightSpan = br.X - top.X;
        if (rightSpan <= CadMath.Epsilon)
        {
            return top.Y;
        }

        var tr = (worldX - top.X) / rightSpan;
        return top.Y + tr * (br.Y - top.Y);
    }

    public static (CadPoint Bl, CadPoint Br, CadPoint Top) BuildInnerTriangle(
        FrameNode frameRoot,
        double frameThickness)
    {
        var inset = FrameProfileGeometry.ResolveInnerInset(
            frameRoot.Width,
            frameRoot.Height,
            frameThickness);
        var outer = BuildOuterTriangle(frameRoot.X, frameRoot.Y, frameRoot.Width, frameRoot.Height);
        return InsetTriangle(outer, inset);
    }

    public static bool ContainsPoint(CadPoint bl, CadPoint br, CadPoint top, double x, double y)
    {
        var p = new CadPoint(x, y);
        var d1 = Cross(bl, br, p);
        var d2 = Cross(br, top, p);
        var d3 = Cross(top, bl, p);
        var hasNeg = d1 < -CadMath.Epsilon || d2 < -CadMath.Epsilon || d3 < -CadMath.Epsilon;
        var hasPos = d1 > CadMath.Epsilon || d2 > CadMath.Epsilon || d3 > CadMath.Epsilon;
        return !(hasNeg && hasPos);
    }

    public static bool ContainsPointInOuterTriangle(FrameNode root, double x, double y)
    {
        if (root.Shape != ConstructionShape.Triangle)
        {
            return x >= root.X && x <= root.X + root.Width && y >= root.Y && y <= root.Y + root.Height;
        }

        var outer = BuildOuterTriangle(root.X, root.Y, root.Width, root.Height);
        return ContainsPoint(outer.Bl, outer.Br, outer.Top, x, y);
    }

    public static bool ContainsPointInInnerTriangle(FrameNode root, double frameThickness, double x, double y)
    {
        if (root.Shape != ConstructionShape.Triangle)
        {
            return ContainsPointInOuterTriangle(root, x, y);
        }

        var inner = BuildInnerTriangle(root, frameThickness);
        return ContainsPoint(inner.Bl, inner.Br, inner.Top, x, y);
    }

    private static double Cross(CadPoint a, CadPoint b, CadPoint c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}
