using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

/// <summary>Точная геометрия отдельных профилей коробки (прямоугольной или скошенной).</summary>
public static class FrameProfileRailGeometry
{
    public readonly record struct RailBand(CadPoint OuterStart, CadPoint OuterEnd, CadPoint InnerEnd, CadPoint InnerStart);

    public readonly record struct OuterCorners(
        CadPoint BottomLeft,
        CadPoint BottomRight,
        CadPoint TopLeft,
        CadPoint TopRight);

    public static OuterCorners ResolveAabbCorners(
        double originX,
        double originY,
        double width,
        double height) =>
        new(
            new CadPoint(originX, originY),
            new CadPoint(originX + width, originY),
            new CadPoint(originX, originY + height),
            new CadPoint(originX + width, originY + height));

    public static RailBand ResolveRailBand(
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        FrameProfileRail rail) =>
        ResolveRailBand(
            ResolveAabbCorners(originX, originY, width, height),
            width,
            height,
            profileThickness,
            rail);

    public static RailBand ResolveRailBand(
        OuterCorners corners,
        double frameWidth,
        double frameHeight,
        double profileThickness,
        FrameProfileRail rail)
    {
        var half = FrameProfileGeometry.ResolveProfileThickness(frameWidth, frameHeight, profileThickness) / 2.0;
        if (half <= CadMath.Epsilon)
        {
            return ResolveDegenerateBand(corners, rail);
        }

        var inner = InsetOuterCorners(corners, half);
        return rail switch
        {
            FrameProfileRail.Top => new RailBand(
                corners.TopLeft,
                corners.TopRight,
                inner.TopRight,
                inner.TopLeft),
            FrameProfileRail.Bottom => new RailBand(
                corners.BottomLeft,
                corners.BottomRight,
                inner.BottomRight,
                inner.BottomLeft),
            FrameProfileRail.Left => new RailBand(
                corners.BottomLeft,
                corners.TopLeft,
                inner.TopLeft,
                inner.BottomLeft),
            FrameProfileRail.Right => new RailBand(
                corners.BottomRight,
                corners.TopRight,
                inner.TopRight,
                inner.BottomRight),
            _ => default
        };
    }

    public static OuterCorners InsetOuterCorners(OuterCorners outer, double inset)
    {
        if (inset <= CadMath.Epsilon)
        {
            return outer;
        }

        // Обход CCW: BL → BR → TR → TL
        var bl = MiterInsetCorner(outer.TopLeft, outer.BottomLeft, outer.BottomRight, inset);
        var br = MiterInsetCorner(outer.BottomLeft, outer.BottomRight, outer.TopRight, inset);
        var tr = MiterInsetCorner(outer.BottomRight, outer.TopRight, outer.TopLeft, inset);
        var tl = MiterInsetCorner(outer.TopRight, outer.TopLeft, outer.BottomLeft, inset);
        return new OuterCorners(bl, br, tl, tr);
    }

    public static IReadOnlyList<CadPoint> InsetPolygon(IReadOnlyList<CadPoint> outerCcw, double inset)
    {
        if (outerCcw.Count < 3 || inset <= CadMath.Epsilon)
        {
            return outerCcw.ToList();
        }

        var result = new List<CadPoint>(outerCcw.Count);
        for (var i = 0; i < outerCcw.Count; i++)
        {
            var previous = outerCcw[(i - 1 + outerCcw.Count) % outerCcw.Count];
            var current = outerCcw[i];
            var next = outerCcw[(i + 1) % outerCcw.Count];
            result.Add(MiterInsetCorner(previous, current, next, inset));
        }

        return result;
    }

    public static bool TryHitFrameRail(
        OuterCorners corners,
        double frameWidth,
        double frameHeight,
        double profileThickness,
        CadPoint point,
        out FrameProfileRail rail)
    {
        rail = default;
        if (frameWidth <= CadMath.Epsilon || frameHeight <= CadMath.Epsilon)
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
            var band = ResolveRailBand(corners, frameWidth, frameHeight, profileThickness, candidate);
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

    public static bool TryHitRectangularFrameRail(
        double originX,
        double originY,
        double width,
        double height,
        double profileThickness,
        CadPoint point,
        out FrameProfileRail rail) =>
        TryHitFrameRail(
            ResolveAabbCorners(originX, originY, width, height),
            width,
            height,
            profileThickness,
            point,
            out rail);

    public static bool ContainsPointInBand(RailBand band, CadPoint point) =>
        ContainsPointInPolygon(
        [
            band.OuterStart,
            band.OuterEnd,
            band.InnerEnd,
            band.InnerStart
        ],
        point);

    /// <summary>
    /// Точка стыка на внешней и внутренней кромках рейки при относительном T (0..1).
    /// </summary>
    public static (CadPoint Outer, CadPoint Inner) ResolveJointPoints(RailBand band, double relativeT)
    {
        var t = Math.Clamp(relativeT, 0, 1);
        var outer = Lerp(band.OuterStart, band.OuterEnd, t);
        var inner = Lerp(band.InnerStart, band.InnerEnd, t);
        return (outer, inner);
    }

    public static CadPoint ResolveJointOuterPoint(
        OuterCorners corners,
        FrameProfileRail rail,
        double relativeT,
        double normalOffsetMm)
    {
        var (start, end) = ResolveOuterEdge(corners, rail);
        var point = Lerp(start, end, Math.Clamp(relativeT, 0, 1));
        var normal = InwardUnitNormal(start, end);
        return new CadPoint(
            CadMath.RoundMm(point.X + normal.X * normalOffsetMm),
            CadMath.RoundMm(point.Y + normal.Y * normalOffsetMm));
    }

    public static (double RelativeT, double NormalOffsetMm) ProjectPointOntoRail(
        OuterCorners corners,
        FrameProfileRail rail,
        CadPoint point)
    {
        var (start, end) = ResolveOuterEdge(corners, rail);
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= CadMath.Epsilon)
        {
            return (0.5, 0);
        }

        var t = Math.Clamp(
            ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / (length * length),
            0.05,
            0.95);
        var basePoint = Lerp(start, end, t);
        var normal = InwardUnitNormal(start, end);
        var offset = (point.X - basePoint.X) * normal.X + (point.Y - basePoint.Y) * normal.Y;
        return (t, CadMath.RoundMm(offset));
    }

    /// <summary>
    /// Строит части ломаной рейки. Внутренние вершины находятся пересечением
    /// параллельно смещённых граней, поэтому в изломе получается корректный miter.
    /// </summary>
    public static IReadOnlyList<RailBand> SplitRailBandWithMiters(
        RailBand baseBand,
        IReadOnlyList<CadPoint> orderedOuterJoints)
    {
        if (orderedOuterJoints.Count == 0)
        {
            return [baseBand];
        }

        var outer = new List<CadPoint>(orderedOuterJoints.Count + 2) { baseBand.OuterStart };
        outer.AddRange(orderedOuterJoints);
        outer.Add(baseBand.OuterEnd);

        var inset = DistancePointToLine(baseBand.InnerStart, baseBand.OuterStart, baseBand.OuterEnd);
        var inner = new CadPoint[outer.Count];
        inner[0] = baseBand.InnerStart;
        inner[^1] = baseBand.InnerEnd;
        var innerTarget = new CadPoint(
            (baseBand.InnerStart.X + baseBand.InnerEnd.X) / 2.0,
            (baseBand.InnerStart.Y + baseBand.InnerEnd.Y) / 2.0);
        for (var i = 1; i < outer.Count - 1; i++)
        {
            inner[i] = MiterOffsetCornerToward(
                outer[i - 1],
                outer[i],
                outer[i + 1],
                inset,
                innerTarget);
        }

        var result = new List<RailBand>(outer.Count - 1);
        for (var i = 0; i < outer.Count - 1; i++)
        {
            result.Add(new RailBand(outer[i], outer[i + 1], inner[i + 1], inner[i]));
        }

        return result;
    }

    /// <summary>
    /// Делит рейку на сегменты по отсортированным стыкам T; возвращает полосы OuterStart→…→OuterEnd.
    /// </summary>
    public static IReadOnlyList<RailBand> SplitRailBand(RailBand band, IReadOnlyList<double> relativeTs)
    {
        var cuts = relativeTs
            .Select(t => Math.Clamp(t, 0, 1))
            .Where(t => t > CadMath.Epsilon && t < 1.0 - CadMath.Epsilon)
            .DistinctBy(t => CadMath.RoundMm(t * 10000))
            .OrderBy(t => t)
            .ToList();

        if (cuts.Count == 0)
        {
            return [band];
        }

        var result = new List<RailBand>(cuts.Count + 1);
        var prevT = 0.0;
        foreach (var t in cuts.Append(1.0))
        {
            var (outerA, innerA) = ResolveJointPoints(band, prevT);
            var (outerB, innerB) = ResolveJointPoints(band, t);
            result.Add(new RailBand(outerA, outerB, innerB, innerA));
            prevT = t;
        }

        return result;
    }

    /// <summary>Длины сегментов по внешней кромке рамы (мм) — истинная длина ребра.</summary>
    public static IReadOnlyList<double> ResolveOuterSegmentLengthsMm(
        double frameWidth,
        double frameHeight,
        FrameProfileRail rail,
        IReadOnlyList<double> relativeTs) =>
        ResolveOuterSegmentLengthsMm(
            ResolveAabbCorners(0, 0, frameWidth, frameHeight),
            rail,
            relativeTs);

    public static IReadOnlyList<double> ResolveOuterSegmentLengthsMm(
        OuterCorners corners,
        FrameProfileRail rail,
        IReadOnlyList<double> relativeTs)
    {
        var (start, end) = ResolveOuterEdge(corners, rail);
        var total = start.DistanceTo(end);
        if (total <= CadMath.Epsilon)
        {
            return [];
        }

        var cuts = NormalizeCuts(relativeTs);
        if (cuts.Count == 0)
        {
            return [CadMath.RoundMm(total)];
        }

        var lengths = new List<double>(cuts.Count + 1);
        var prev = 0.0;
        foreach (var t in cuts.Append(1.0))
        {
            lengths.Add(CadMath.RoundMm((t - prev) * total));
            prev = t;
        }

        var sum = lengths.Sum();
        var delta = CadMath.RoundMm(total - sum);
        if (Math.Abs(delta) > CadMath.Epsilon && lengths.Count > 0)
        {
            lengths[^1] = CadMath.RoundMm(lengths[^1] + delta);
        }

        return lengths;
    }

    /// <summary>
    /// Ортогональные проекции сегментов: |ΔX| для горизонтоподобных реек, |ΔY| для вертикалеподобных.
    /// </summary>
    public static IReadOnlyList<double> ResolveOuterSegmentProjectionsMm(
        OuterCorners corners,
        FrameProfileRail rail,
        IReadOnlyList<double> relativeTs)
    {
        var (start, end) = ResolveOuterEdge(corners, rail);
        var useHorizontal = rail is FrameProfileRail.Top or FrameProfileRail.Bottom
            || Math.Abs(end.X - start.X) >= Math.Abs(end.Y - start.Y);
        var total = useHorizontal
            ? Math.Abs(end.X - start.X)
            : Math.Abs(end.Y - start.Y);
        if (total <= CadMath.Epsilon)
        {
            return [];
        }

        var cuts = NormalizeCuts(relativeTs);
        if (cuts.Count == 0)
        {
            return [CadMath.RoundMm(total)];
        }

        var lengths = new List<double>(cuts.Count + 1);
        var prev = 0.0;
        foreach (var t in cuts.Append(1.0))
        {
            var a = Lerp(start, end, prev);
            var b = Lerp(start, end, t);
            var segment = useHorizontal
                ? Math.Abs(b.X - a.X)
                : Math.Abs(b.Y - a.Y);
            lengths.Add(CadMath.RoundMm(segment));
            prev = t;
        }

        var sum = lengths.Sum();
        var delta = CadMath.RoundMm(total - sum);
        if (Math.Abs(delta) > CadMath.Epsilon && lengths.Count > 0)
        {
            lengths[^1] = CadMath.RoundMm(lengths[^1] + delta);
        }

        return lengths;
    }

    /// <summary>Мировые координаты границ сегментов вдоль внешней оси рейки (включая концы).</summary>
    public static IReadOnlyList<double> ResolveOuterBoundaryCoordinates(
        double originX,
        double originY,
        double width,
        double height,
        FrameProfileRail rail,
        IReadOnlyList<double> relativeTs) =>
        ResolveOuterBoundaryCoordinates(
            ResolveAabbCorners(originX, originY, width, height),
            rail,
            relativeTs);

    /// <summary>
    /// Границы сегментов: для горизонтоподобных — уникальные X, для вертикалеподобных — уникальные Y.
    /// </summary>
    public static IReadOnlyList<double> ResolveOuterBoundaryCoordinates(
        OuterCorners corners,
        FrameProfileRail rail,
        IReadOnlyList<double> relativeTs)
    {
        var (start, end) = ResolveOuterEdge(corners, rail);
        var useHorizontal = rail is FrameProfileRail.Top or FrameProfileRail.Bottom
            || Math.Abs(end.X - start.X) >= Math.Abs(end.Y - start.Y);
        var cuts = NormalizeCuts(relativeTs);
        var list = new List<double>(cuts.Count + 2);
        list.Add(CadMath.RoundMm(useHorizontal ? start.X : start.Y));
        foreach (var t in cuts)
        {
            var p = Lerp(start, end, t);
            list.Add(CadMath.RoundMm(useHorizontal ? p.X : p.Y));
        }

        list.Add(CadMath.RoundMm(useHorizontal ? end.X : end.Y));
        return list;
    }

    public static (CadPoint Start, CadPoint End) ResolveOuterEdge(OuterCorners corners, FrameProfileRail rail) =>
        rail switch
        {
            FrameProfileRail.Top => (corners.TopLeft, corners.TopRight),
            FrameProfileRail.Bottom => (corners.BottomLeft, corners.BottomRight),
            FrameProfileRail.Left => (corners.BottomLeft, corners.TopLeft),
            FrameProfileRail.Right => (corners.BottomRight, corners.TopRight),
            _ => (default, default)
        };

    /// <summary>Проекция точки на внешнее ребро рейки → RelativeT (0..1).</summary>
    public static double ProjectPointOntoRailT(OuterCorners corners, FrameProfileRail rail, CadPoint point)
    {
        var (start, end) = ResolveOuterEdge(corners, rail);
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lenSq = dx * dx + dy * dy;
        if (lenSq <= CadMath.Epsilon)
        {
            return 0.5;
        }

        var t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lenSq;
        return Math.Clamp(t, 0.05, 0.95);
    }

    private static RailBand ResolveDegenerateBand(OuterCorners corners, FrameProfileRail rail) =>
        rail switch
        {
            FrameProfileRail.Top => new RailBand(corners.TopLeft, corners.TopRight, corners.TopRight, corners.TopLeft),
            FrameProfileRail.Bottom => new RailBand(corners.BottomLeft, corners.BottomRight, corners.BottomRight, corners.BottomLeft),
            FrameProfileRail.Left => new RailBand(corners.BottomLeft, corners.TopLeft, corners.TopLeft, corners.BottomLeft),
            FrameProfileRail.Right => new RailBand(corners.BottomRight, corners.TopRight, corners.TopRight, corners.BottomRight),
            _ => default
        };

    /// <summary>
    /// Вставка угла: пересечение двух смещённых внутрь рёбер prev→curr и curr→next (CCW).
    /// </summary>
    private static CadPoint MiterInsetCorner(CadPoint prev, CadPoint curr, CadPoint next, double inset)
    {
        var n1 = InwardUnitNormal(prev, curr);
        var n2 = InwardUnitNormal(curr, next);
        var a0 = new CadPoint(prev.X + n1.X * inset, prev.Y + n1.Y * inset);
        var a1 = new CadPoint(curr.X + n1.X * inset, curr.Y + n1.Y * inset);
        var b0 = new CadPoint(curr.X + n2.X * inset, curr.Y + n2.Y * inset);
        var b1 = new CadPoint(next.X + n2.X * inset, next.Y + n2.Y * inset);
        if (TryIntersectLines(a0, a1, b0, b1, out var intersection))
        {
            return new CadPoint(CadMath.RoundMm(intersection.X), CadMath.RoundMm(intersection.Y));
        }

        var nx = (n1.X + n2.X) * 0.5;
        var ny = (n1.Y + n2.Y) * 0.5;
        var nLen = Math.Sqrt(nx * nx + ny * ny);
        if (nLen > CadMath.Epsilon)
        {
            nx /= nLen;
            ny /= nLen;
        }

        return new CadPoint(
            CadMath.RoundMm(curr.X + nx * inset),
            CadMath.RoundMm(curr.Y + ny * inset));
    }

    private static CadPoint MiterOffsetCornerToward(
        CadPoint prev,
        CadPoint curr,
        CadPoint next,
        double inset,
        CadPoint target)
    {
        var n1 = UnitNormalToward(prev, curr, target);
        var n2 = UnitNormalToward(curr, next, target);
        var a0 = new CadPoint(prev.X + n1.X * inset, prev.Y + n1.Y * inset);
        var a1 = new CadPoint(curr.X + n1.X * inset, curr.Y + n1.Y * inset);
        var b0 = new CadPoint(curr.X + n2.X * inset, curr.Y + n2.Y * inset);
        var b1 = new CadPoint(next.X + n2.X * inset, next.Y + n2.Y * inset);
        if (TryIntersectLines(a0, a1, b0, b1, out var intersection))
        {
            return new CadPoint(CadMath.RoundMm(intersection.X), CadMath.RoundMm(intersection.Y));
        }

        return new CadPoint(
            CadMath.RoundMm(curr.X + (n1.X + n2.X) * inset / 2.0),
            CadMath.RoundMm(curr.Y + (n1.Y + n2.Y) * inset / 2.0));
    }

    private static (double X, double Y) UnitNormalToward(CadPoint from, CadPoint to, CadPoint target)
    {
        var normal = InwardUnitNormal(from, to);
        var midpointX = (from.X + to.X) / 2.0;
        var midpointY = (from.Y + to.Y) / 2.0;
        var dot = normal.X * (target.X - midpointX) + normal.Y * (target.Y - midpointY);
        return dot >= 0 ? normal : (-normal.X, -normal.Y);
    }

    /// <summary>Внутренняя нормаль для CCW-обхода: (-dy, dx).</summary>
    private static (double X, double Y) InwardUnitNormal(CadPoint from, CadPoint to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len <= CadMath.Epsilon)
        {
            return (0, 0);
        }

        return (-dy / len, dx / len);
    }

    private static bool TryIntersectLines(
        CadPoint a0,
        CadPoint a1,
        CadPoint b0,
        CadPoint b1,
        out CadPoint intersection)
    {
        var dxA = a1.X - a0.X;
        var dyA = a1.Y - a0.Y;
        var dxB = b1.X - b0.X;
        var dyB = b1.Y - b0.Y;
        var denom = dxA * dyB - dyA * dxB;
        if (Math.Abs(denom) <= CadMath.Epsilon)
        {
            intersection = default;
            return false;
        }

        var t = ((b0.X - a0.X) * dyB - (b0.Y - a0.Y) * dxB) / denom;
        intersection = new CadPoint(a0.X + t * dxA, a0.Y + t * dyA);
        return true;
    }

    private static double DistancePointToLine(CadPoint point, CadPoint lineA, CadPoint lineB)
    {
        var dx = lineB.X - lineA.X;
        var dy = lineB.Y - lineA.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        return length <= CadMath.Epsilon
            ? 0
            : Math.Abs(dx * (lineA.Y - point.Y) - (lineA.X - point.X) * dy) / length;
    }

    private static List<double> NormalizeCuts(IReadOnlyList<double> relativeTs) =>
        relativeTs
            .Select(t => Math.Clamp(t, 0, 1))
            .Where(t => t > CadMath.Epsilon && t < 1.0 - CadMath.Epsilon)
            .DistinctBy(t => CadMath.RoundMm(t * 10000))
            .OrderBy(t => t)
            .ToList();

    private static CadPoint Lerp(CadPoint a, CadPoint b, double t) =>
        new(
            CadMath.RoundMm(a.X + (b.X - a.X) * t),
            CadMath.RoundMm(a.Y + (b.Y - a.Y) * t));

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
