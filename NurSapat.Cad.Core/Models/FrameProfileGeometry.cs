using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Монолитная геометрия профиля арочной рамы: прямоугольная коробка + концентрическая арка.
/// Внешний и внутренний контуры — замкнутые циклы с единым центром O и общими углами дуги.
/// </summary>
public static class FrameProfileGeometry
{
    public const double ArcTessellationRadians = Math.PI / 24.0;
    public const int ArcTessellationSegments = 24;

    public static double ResolveProfileThickness(double width, double height, double profileThickness)
    {
        if (profileThickness <= CadMath.Epsilon)
        {
            return 0;
        }

        var maxT = Math.Min(width, height) * 0.45;
        if (maxT <= 2)
        {
            return maxT;
        }

        return Math.Clamp(profileThickness, 2, maxT);
    }

    public static double ResolveInnerInset(double width, double height, double profileThickness) =>
        ResolveProfileThickness(width, height, profileThickness) / 2.0;

    /// <summary>R_inner = R_outer − profileWall (единая формула для всех производных контуров).</summary>
    public static double ResolveInnerRadiusFromOuter(double outerRadius, double profileWallMm) =>
        Math.Max(outerRadius - profileWallMm, CadMath.Epsilon);

    /// <summary>
    /// Аналитические параметры арки: единый центр O, концентрические радиусы, углы хорды.
    /// R_outer = H/2 + W²/(8H); Y_c = Y_chord + H − R_outer; R_inner = R_outer − profileWall.
    /// </summary>
    public static ArcSpringingGeometry ComputeArcSpringingGeometry(
        double originX,
        double width,
        double chordY,
        double arcHeight,
        double profileWallMm)
    {
        var w = width;
        var h = arcHeight;
        if (w <= CadMath.Epsilon || h <= ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            throw new InvalidOperationException("Arc geometry requires positive width and sagitta.");
        }

        var rOuter = ProfileSegment.ComputeRadiusFromSagitta(w, h);
        var centerX = originX + w / 2.0;
        var centerY = chordY + h - rOuter;
        var center = new CadPoint(centerX, centerY);

        var halfW = w / 2.0;
        var dy = chordY - centerY;
        var startAngle = Math.Atan2(dy, -halfW);
        var endAngle = Math.Atan2(dy, halfW);
        var rInner = ResolveInnerRadiusFromOuter(rOuter, profileWallMm);

        return new ArcSpringingGeometry(
            center,
            chordY,
            rOuter,
            rInner,
            startAngle,
            endAngle,
            profileWallMm);
    }

    /// <summary>
    /// Строит замкнутые внешний и внутренний контуры монолитной арочной рамы.
    /// Коробка: левый нижний → левая стойка → дуга → правая стойка → низ.
    /// </summary>
    public static ArchedFrameProfileContours BuildArchedFrameContours(
        double originX,
        double originY,
        double width,
        double boxHeight,
        ProfileSegment topProfile,
        double profileThickness)
    {
        if (width <= CadMath.Epsilon || boxHeight <= CadMath.Epsilon)
        {
            throw new InvalidOperationException("Frame requires positive width and box height.");
        }

        var profileWall = ResolveInnerInset(width, boxHeight, profileThickness);
        var arcHeight = topProfile.ArcHeight;
        if (topProfile.SegmentType != SegmentType.Arc ||
            arcHeight <= ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            throw new InvalidOperationException("Arched frame contours require an arc top profile.");
        }

        var x0 = originX;
        var y0 = originY;
        var x1 = originX + width;
        var chordY = originY + boxHeight;

        var arc = ComputeArcSpringingGeometry(x0, width, chordY, arcHeight, profileWall);

        var outerBottomLeft = new CadPoint(x0, y0);
        var outerSpringingLeft = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.OuterRadius, x0);
        var outerSpringingRight = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.OuterRadius, x1);
        var outerBottomRight = new CadPoint(x1, y0);

        var outerLoop = ProfileContourLoop.FromEdges(
            ProfileEdge.Line(outerBottomLeft, outerSpringingLeft),
            ProfileEdge.Arc(arc.Center, arc.OuterRadius, arc.StartAngleRad, arc.EndAngleRad),
            ProfileEdge.Line(outerSpringingRight, outerBottomRight),
            ProfileEdge.Line(outerBottomRight, outerBottomLeft));

        var xi0 = x0 + profileWall;
        var xi1 = x1 - profileWall;
        var yi0 = y0 + profileWall;

        var innerSpringingLeft = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, xi0);
        var innerSpringingRight = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, xi1);
        var innerBottomLeft = new CadPoint(xi0, yi0);
        var innerBottomRight = new CadPoint(xi1, yi0);

        var innerStartAngle = CircularArc.AngleRad(arc.Center, innerSpringingLeft);
        var innerEndAngle = CircularArc.AngleRad(arc.Center, innerSpringingRight);

        var innerLoop = ProfileContourLoop.FromEdges(
            ProfileEdge.Line(innerBottomLeft, innerSpringingLeft),
            ProfileEdge.Arc(arc.Center, arc.InnerRadius, innerStartAngle, innerEndAngle),
            ProfileEdge.Line(innerSpringingRight, innerBottomRight),
            ProfileEdge.Line(innerBottomRight, innerBottomLeft));

        return new ArchedFrameProfileContours(
            outerLoop,
            innerLoop,
            innerLoop,
            arc.Center,
            arc.OuterRadius,
            arc.InnerRadius,
            arc.StartAngleRad,
            arc.EndAngleRad,
            outerSpringingLeft,
            innerSpringingLeft,
            outerSpringingRight,
            innerSpringingRight);
    }

    /// <summary>Средний угол верхней ветви дуги (для размерной линии радиуса).</summary>
    public static double ComputeArcMidAngleRad(double startAngleRad, double endAngleRad)
    {
        var cwSpan = ProfileSegment.PositiveAngleDelta(endAngleRad, startAngleRad);
        return NormalizeAngleRad(startAngleRad - cwSpan / 2.0);
    }

    public static double NormalizeAngleRad(double angleRad)
    {
        while (angleRad < 0)
        {
            angleRad += Math.PI * 2.0;
        }

        while (angleRad >= Math.PI * 2.0)
        {
            angleRad -= Math.PI * 2.0;
        }

        return angleRad;
    }

    /// <summary>
    /// Параметры внутренней дуги арочной рамы (единый центр O, R_inner).
    /// </summary>
    public static bool TryGetArchedFrameInnerArc(
        FrameNode frameRoot,
        double frameThickness,
        out ArcSpringingGeometry arc)
    {
        arc = default;
        var profile = frameRoot.TopProfile;
        if (profile?.SegmentType != SegmentType.Arc ||
            profile.ArcHeight <= ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return false;
        }

        var chordY = frameRoot.Y + frameRoot.Height;
        var profileWall = ResolveInnerInset(frameRoot.Width, frameRoot.Height, frameThickness);
        arc = ComputeArcSpringingGeometry(
            frameRoot.X,
            frameRoot.Width,
            chordY,
            profile.ArcHeight,
            profileWall);
        return true;
    }

    /// <summary>
    /// Y верхней ветви внутренней дуги при x = const (с ограничением у краёв арки).
    /// </summary>
    public static bool TryResolveInnerArcTopYAtX(
        FrameNode frameRoot,
        double frameThickness,
        double worldX,
        out double topY)
    {
        topY = 0;
        if (!TryGetArchedFrameInnerArc(frameRoot, frameThickness, out var arc))
        {
            return false;
        }

        topY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, worldX).Y;
        return true;
    }

    /// <summary>
    /// Горизонтальный уровень worldY в зоне арки: span по X на внутренней дуге (ветвь выше хорды).
    /// </summary>
    public static bool TryResolveInnerArcHorizontalSpanAtY(
        ArcSpringingGeometry arc,
        double worldY,
        double clampMinX,
        double clampMaxX,
        out double leftX,
        out double rightX)
    {
        leftX = rightX = 0;
        if (worldY <= arc.ChordY + CadMath.MmPrecision)
        {
            return false;
        }

        var dy = worldY - arc.Center.Y;
        if (dy < -CadMath.Epsilon)
        {
            return false;
        }

        var underRoot = arc.InnerRadius * arc.InnerRadius - dy * dy;
        if (underRoot < -CadMath.Epsilon)
        {
            return false;
        }

        var halfSpan = Math.Sqrt(Math.Max(0, underRoot));
        leftX = CadMath.RoundMm(Math.Max(clampMinX, arc.Center.X - halfSpan));
        rightX = CadMath.RoundMm(Math.Min(clampMaxX, arc.Center.X + halfSpan));
        return rightX - leftX > CadMath.Epsilon;
    }
}

/// <summary>Параметры арки с единым центром O.</summary>
public readonly record struct ArcSpringingGeometry(
    CadPoint Center,
    double ChordY,
    double OuterRadius,
    double InnerRadius,
    double StartAngleRad,
    double EndAngleRad,
    double ProfileWallMm);

/// <summary>Замкнутые контуры профиля арочной рамы и параметры дуги.</summary>
public sealed record ArchedFrameProfileContours(
    ProfileContourLoop OuterLoop,
    ProfileContourLoop InnerLoop,
    ProfileContourLoop OpeningLoop,
    CadPoint ArcCenter,
    double OuterArcRadius,
    double InnerArcRadius,
    double OuterArcStartAngleRad,
    double OuterArcEndAngleRad,
    CadPoint OuterSpringingLeft,
    CadPoint InnerSpringingLeft,
    CadPoint OuterSpringingRight,
    CadPoint InnerSpringingRight)
{
    public double OuterArcMidAngleRad =>
        FrameProfileGeometry.ComputeArcMidAngleRad(OuterArcStartAngleRad, OuterArcEndAngleRad);

    public CadPoint OuterArcMidPoint =>
        CircularArc.PointAtAngle(ArcCenter, OuterArcRadius, OuterArcMidAngleRad);
}

public enum ProfileEdgeKind
{
    Line,
    Arc
}

/// <summary>Прямой участок или дуга окружности в контуре профиля.</summary>
public readonly record struct ProfileEdge(
    ProfileEdgeKind Kind,
    CadPoint Start,
    CadPoint End,
    CadPoint ArcCenter,
    double ArcRadius,
    double ArcStartAngleRad,
    double ArcEndAngleRad)
{
    public static ProfileEdge Line(CadPoint start, CadPoint end) =>
        new(ProfileEdgeKind.Line, start, end, default, 0, 0, 0);

    public static ProfileEdge Arc(CadPoint center, double radius, double startAngleRad, double endAngleRad)
    {
        var start = CircularArc.PointAtAngle(center, radius, startAngleRad);
        var end = CircularArc.PointAtAngle(center, radius, endAngleRad);
        return new(ProfileEdgeKind.Arc, start, end, center, radius, startAngleRad, endAngleRad);
    }
}

/// <summary>Замкнутый контур профиля — последовательность рёбер с общими вершинами.</summary>
public sealed class ProfileContourLoop
{
    public IReadOnlyList<ProfileEdge> Edges { get; }

    private ProfileContourLoop(IReadOnlyList<ProfileEdge> edges)
    {
        Edges = edges;
    }

    public static ProfileContourLoop FromEdges(params ProfileEdge[] edges) =>
        FromEdges((IEnumerable<ProfileEdge>)edges);

    public static ProfileContourLoop FromEdges(IEnumerable<ProfileEdge> edges)
    {
        var list = edges.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("Contour requires at least one edge.", nameof(edges));
        }

        for (var i = 0; i < list.Count; i++)
        {
            var current = list[i];
            var next = list[(i + 1) % list.Count];
            if (!current.End.IsNear(next.Start, CadMath.MmPrecision))
            {
                throw new InvalidOperationException(
                    $"Contour edge {i} ends at {current.End} but edge {(i + 1) % list.Count} starts at {next.Start}.");
            }
        }

        return new ProfileContourLoop(list);
    }

    public IReadOnlyList<CadPoint> Tessellate(
        double arcStepRadians = FrameProfileGeometry.ArcTessellationRadians,
        int arcSegments = FrameProfileGeometry.ArcTessellationSegments) =>
        TessellateEdges(Edges, arcStepRadians, arcSegments);

    private static List<CadPoint> TessellateEdges(
        IReadOnlyList<ProfileEdge> edges,
        double arcStepRadians,
        int arcSegments)
    {
        var points = new List<CadPoint>();
        foreach (var edge in edges)
        {
            if (points.Count == 0)
            {
                points.Add(edge.Start);
            }

            if (edge.Kind == ProfileEdgeKind.Line)
            {
                points.Add(edge.End);
                continue;
            }

            foreach (var point in CircularArc.TessellateSpringingArc(
                         edge.ArcCenter,
                         edge.ArcRadius,
                         edge.ArcStartAngleRad,
                         edge.ArcEndAngleRad,
                         arcStepRadians,
                         arcSegments))
            {
                points.Add(point);
            }
        }

        return points;
    }
}

/// <summary>Дуга окружности — аналитическое представление.</summary>
public static class CircularArc
{
    public static double AngleRad(CadPoint center, CadPoint point) =>
        Math.Atan2(point.Y - center.Y, point.X - center.X);

    public static CadPoint PointAtAngle(CadPoint center, double radius, double angleRad) =>
        new(
            center.X + radius * Math.Cos(angleRad),
            center.Y + radius * Math.Sin(angleRad));

    public static bool TryIntersectVertical(
        CadPoint center,
        double radius,
        double verticalX,
        out CadPoint lower,
        out CadPoint upper)
    {
        var dx = verticalX - center.X;
        var underRoot = radius * radius - dx * dx;
        if (underRoot < -CadMath.Epsilon)
        {
            lower = default;
            upper = default;
            return false;
        }

        var root = Math.Sqrt(Math.Max(0, underRoot));
        lower = new CadPoint(verticalX, center.Y - root);
        upper = new CadPoint(verticalX, center.Y + root);
        return true;
    }

    /// <summary>
    /// Тесселяция верхней ветви дуги от левой точки хорды (startAngle) к правой (endAngle).
    /// Для арочного окна обход идёт по короткой верхней дуге (по часовой в мировых координатах).
    /// </summary>
    public static IReadOnlyList<CadPoint> TessellateSpringingArc(
        CadPoint center,
        double radius,
        double startAngleRad,
        double endAngleRad,
        double stepRadians = FrameProfileGeometry.ArcTessellationRadians,
        int segments = FrameProfileGeometry.ArcTessellationSegments)
    {
        if (radius <= CadMath.Epsilon)
        {
            return Array.Empty<CadPoint>();
        }

        var cwSpan = ProfileSegment.PositiveAngleDelta(endAngleRad, startAngleRad);
        if (cwSpan <= CadMath.Epsilon)
        {
            return Array.Empty<CadPoint>();
        }

        var segmentCount = segments > 0
            ? segments
            : Math.Max(1, (int)Math.Ceiling(cwSpan / stepRadians));
        var points = new List<CadPoint>(segmentCount);

        for (var i = 1; i <= segmentCount; i++)
        {
            var t = i / (double)segmentCount;
            var angle = startAngleRad - cwSpan * t;
            points.Add(PointAtAngle(center, radius, angle));
        }

        return points;
    }

    /// <summary>
    /// Общая тесселяция дуги: выбирает короткую дугу (CCW), если span ≤ π, иначе верхнюю ветвь (CW).
    /// </summary>
    public static IReadOnlyList<CadPoint> Tessellate(
        CadPoint center,
        double radius,
        double startAngleRad,
        double endAngleRad,
        double stepRadians = FrameProfileGeometry.ArcTessellationRadians,
        int segments = FrameProfileGeometry.ArcTessellationSegments)
    {
        if (radius <= CadMath.Epsilon)
        {
            return Array.Empty<CadPoint>();
        }

        var ccwSpan = ProfileSegment.PositiveAngleDelta(startAngleRad, endAngleRad);
        if (ccwSpan <= CadMath.Epsilon)
        {
            return Array.Empty<CadPoint>();
        }

        if (ccwSpan > Math.PI + CadMath.Epsilon)
        {
            return TessellateSpringingArc(center, radius, startAngleRad, endAngleRad, stepRadians, segments);
        }

        var segmentCount = segments > 0
            ? segments
            : Math.Max(1, (int)Math.Ceiling(ccwSpan / stepRadians));
        var points = new List<CadPoint>(segmentCount);

        for (var i = 1; i <= segmentCount; i++)
        {
            var t = i / (double)segmentCount;
            var angle = startAngleRad + ccwSpan * t;
            points.Add(PointAtAngle(center, radius, angle));
        }

        return points;
    }
}
