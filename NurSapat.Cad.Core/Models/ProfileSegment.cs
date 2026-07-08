using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

public enum SegmentType
{
    Line,
    Arc
}

/// <summary>
/// Сегмент профиля рамы (прямой отрезок или дуга) с двусторонней связью радиуса и высоты стрелы прогиба.
/// Арочная геометрия строится по правилу единого центра: внешняя и внутренняя дуги концентричны.
/// </summary>
public sealed partial class ProfileSegment : ObservableObject
{
    private bool _isSyncing;

    /// <summary>Начальный радиус при первом преобразовании Line → Arc (полукруг при W=1000 → R=500, H=500).</summary>
    public const double DefaultInitRadiusMm = 500.0;

    [ObservableProperty]
    private SegmentType _segmentType = SegmentType.Line;

    [ObservableProperty]
    private double _startX;

    [ObservableProperty]
    private double _startY;

    [ObservableProperty]
    private double _endX;

    [ObservableProperty]
    private double _endY;

    private double _radius;

    private double _arcHeight;

    public double ChordWidth =>
        Math.Sqrt(Math.Pow(EndX - StartX, 2) + Math.Pow(EndY - StartY, 2));

    public double MinimumRadius => ChordWidth / 2.0;

    /// <summary>R_outer = (H/2) + W²/(8H) для стрелы прогиба H и ширины хорды W.</summary>
    public static double ComputeRadiusFromSagitta(double chordWidth, double arcHeight) =>
        (arcHeight / 2.0) + (chordWidth * chordWidth) / (8.0 * arcHeight);

    /// <summary>H = R − sqrt(R² − (W/2)²) для радиуса R и ширины хорды W.</summary>
    public static double ComputeSagittaFromRadius(double chordWidth, double radius)
    {
        var halfW = chordWidth / 2.0;
        var underRoot = Math.Max(0, radius * radius - halfW * halfW);
        return radius - Math.Sqrt(underRoot);
    }

    public double Radius
    {
        get => _radius;
        set
        {
            if (SegmentType != SegmentType.Arc || ChordWidth <= CadMath.Epsilon)
            {
                var lineRadius = Math.Max(0, value);
                if (Math.Abs(_radius - lineRadius) < CadMath.Epsilon)
                {
                    return;
                }

                _radius = lineRadius;
                OnPropertyChanged(nameof(Radius));
                return;
            }

            var clamped = ClampRadius(value);
            var radiusChanged = Math.Abs(_radius - clamped) >= CadMath.Epsilon;
            if (radiusChanged)
            {
                _radius = clamped;
                OnPropertyChanged(nameof(Radius));
            }

            if (_isSyncing)
            {
                return;
            }

            SyncArcHeightFromRadius();
        }
    }

    public double ArcHeight
    {
        get => _arcHeight;
        set
        {
            if (SegmentType != SegmentType.Arc)
            {
                return;
            }

            if (value <= MinFlatArcHeightMm + CadMath.Epsilon)
            {
                ConvertToLine();
                return;
            }

            var clamped = ClampArcHeight(value);
            var heightChanged = Math.Abs(_arcHeight - clamped) >= CadMath.Epsilon;
            if (heightChanged)
            {
                _arcHeight = clamped;
                OnPropertyChanged(nameof(ArcHeight));
            }

            if (_isSyncing)
            {
                return;
            }

            SyncRadiusFromArcHeight();
        }
    }

    private void SyncArcHeightFromRadius()
    {
        var targetHeight = ClampArcHeight(ComputeSagittaFromRadius(ChordWidth, _radius));
        if (Math.Abs(_arcHeight - targetHeight) < CadMath.Epsilon)
        {
            return;
        }

        _isSyncing = true;
        _arcHeight = targetHeight;
        OnPropertyChanged(nameof(ArcHeight));
        OnPropertyChanged(nameof(IsLargeArc));
        _isSyncing = false;
    }

    private void SyncRadiusFromArcHeight()
    {
        var targetRadius = Math.Max(MinimumRadius, ComputeRadiusFromSagitta(ChordWidth, _arcHeight));
        targetRadius = ClampRadius(targetRadius);
        if (Math.Abs(_radius - targetRadius) < CadMath.Epsilon)
        {
            return;
        }

        _isSyncing = true;
        _radius = targetRadius;
        OnPropertyChanged(nameof(Radius));
        OnPropertyChanged(nameof(IsLargeArc));
        _isSyncing = false;
    }

    public bool IsLargeArc =>
        SegmentType == SegmentType.Arc &&
        ArcHeight > Radius + CadMath.Epsilon;

    public CadPoint Start => new(StartX, StartY);

    public CadPoint End => new(EndX, EndY);

    public static ProfileSegment CreateTopRail(double originX, double originY, double width, double height) =>
        new()
        {
            SegmentType = SegmentType.Line,
            StartX = originX,
            StartY = originY + height,
            EndX = originX + width,
            EndY = originY + height
        };

    public void SyncTopRailGeometry(double originX, double originY, double width, double height)
    {
        StartX = originX;
        StartY = originY + height;
        EndX = originX + width;
        EndY = originY + height;

        if (SegmentType == SegmentType.Arc && ChordWidth > CadMath.Epsilon)
        {
            var previousHeight = _arcHeight;
            if (previousHeight > CadMath.Epsilon)
            {
                ArcHeight = ClampArcHeight(previousHeight);
            }
        }
    }

    /// <summary>Ниже этого порога стрела считается нулевой — сегмент возвращается к прямой линии.</summary>
    public const double MinFlatArcHeightMm = 0.1;

    /// <summary>Минимально безопасная стрела прогиба (мм) — ниже нарушается сопряжение профиля.</summary>
    public const double MinSafeArcHeightMm = 100.0;

    /// <summary>Максимальная стрела — полукруг при R = W/2.</summary>
    public double MaximumArcHeight => MinimumRadius;

    /// <summary>Максимальный радиус при минимально безопасной стреле MinSafeArcHeightMm.</summary>
    public double MaximumRadius =>
        ChordWidth <= CadMath.Epsilon
            ? double.MaxValue
            : ComputeRadiusFromSagitta(ChordWidth, MinSafeArcHeightMm);

    public double ClampRadius(double radius) =>
        Math.Clamp(radius, MinimumRadius, MaximumRadius);

    public double ClampArcHeight(double arcHeight)
    {
        var safeMin = Math.Min(MinSafeArcHeightMm, MaximumArcHeight);
        return Math.Clamp(arcHeight, safeMin, MaximumArcHeight);
    }

    /// <summary>
    /// Единый центр окружности O(X_c, Y_c) для внешней и внутренней дуг.
    /// X_c = x + W/2; Y_c = Y_chord + H − R_outer; R_outer = H/2 + W²/(8H).
    /// </summary>
    public CadPoint GetArcCenterWorld()
    {
        var chordY = (StartY + EndY) / 2.0;
        var chordWidth = ChordWidth;
        var rOuter = ArcHeight > CadMath.Epsilon
            ? ComputeRadiusFromSagitta(chordWidth, ArcHeight)
            : Radius;
        return new CadPoint(
            (StartX + EndX) / 2.0,
            chordY + ArcHeight - rOuter);
    }

    /// <summary>Углы внешней дуги относительно O для линии хорды Y_chord.</summary>
    public (double StartAngleRad, double EndAngleRad) GetOuterArcAnglesRad()
    {
        var center = GetArcCenterWorld();
        var chordY = (StartY + EndY) / 2.0;
        var halfW = ChordWidth / 2.0;
        return (
            Math.Atan2(chordY - center.Y, -halfW),
            Math.Atan2(chordY - center.Y, halfW));
    }

    public CadPoint GetArcApexWorld()
    {
        var center = GetArcCenterWorld();
        var outerRadius = ArcHeight > CadMath.Epsilon
            ? ComputeRadiusFromSagitta(ChordWidth, ArcHeight)
            : Radius;
        return CircularArc.PointAtAngle(center, outerRadius, Math.PI / 2.0);
    }

    /// <summary>Ручка управления стрелой прогиба на линии начала арки (хорда, переход прямоугольник → дуга).</summary>
    public CadPoint GetArcSpringingHandleWorld()
    {
        var chordY = (StartY + EndY) / 2.0;
        return new CadPoint((StartX + EndX) / 2.0, chordY);
    }

    /// <summary>Точка верхней ветви окружности (мир, Y вверх) при заданном X.</summary>
    public static CadPoint GetPointOnUpperArc(CadPoint center, double radius, double x)
    {
        var dx = x - center.X;
        var underRoot = radius * radius - dx * dx;
        if (underRoot >= -CadMath.Epsilon)
        {
            return new CadPoint(x, center.Y + Math.Sqrt(Math.Max(0, underRoot)));
        }

        var side = x < center.X ? -1.0 : 1.0;
        return new CadPoint(center.X + side * radius, center.Y);
    }

    public CadPoint GetPointOnUpperArc(double x, double radius) =>
        GetPointOnUpperArc(GetArcCenterWorld(), radius, x);

    /// <summary>Угол (рад) от центра O к точке на дуге.</summary>
    public static double GetAngleRad(CadPoint center, CadPoint point) =>
        Math.Atan2(point.Y - center.Y, point.X - center.X);

    /// <summary>Вертикаль x пересекает верхнюю ветвь окружности (|x − X_c| ≤ R).</summary>
    public static bool VerticalIntersectsUpperArc(CadPoint center, double radius, double verticalX) =>
        Math.Abs(verticalX - center.X) <= radius + CadMath.Epsilon;

    /// <summary>
    /// Пересечение вертикали x = const с верхней ветвью окружности (O, R).
    /// </summary>
    public static bool TryIntersectVerticalWithUpperArc(
        CadPoint center,
        double radius,
        double verticalX,
        out CadPoint intersection)
    {
        if (!VerticalIntersectsUpperArc(center, radius, verticalX))
        {
            intersection = default;
            return false;
        }

        intersection = GetPointOnUpperArc(center, radius, verticalX);
        return true;
    }

    /// <summary>Крайняя точка верхней дуги по горизонтали (левая или правая).</summary>
    public static CadPoint GetUpperArcHorizontalExtent(CadPoint center, double radius, bool leftSide) =>
        CircularArc.PointAtAngle(center, radius, leftSide ? Math.PI : 0);

    /// <summary>
    /// Параметры обхода верхней ветви дуги с известным центром O.
    /// </summary>
    public static (bool IsLargeArc, bool CounterClockwiseInWorld) ResolveUpperBranchArcSweep(
        CadPoint center,
        double radius,
        CadPoint arcStart,
        CadPoint arcEnd)
    {
        _ = radius;
        var angleStart = GetAngleRad(center, arcStart);
        var angleEnd = GetAngleRad(center, arcEnd);
        const double angleApex = Math.PI / 2.0;

        var ccwSpan = PositiveAngleDelta(angleStart, angleEnd);
        if (AngleInCounterClockwiseRange(angleStart, angleEnd, angleApex))
        {
            return (ccwSpan > Math.PI + CadMath.Epsilon, true);
        }

        var cwSpan = PositiveAngleDelta(angleEnd, angleStart);
        return (cwSpan > Math.PI + CadMath.Epsilon, false);
    }

    /// <summary>Нормализованный положительный угол от from до to (против часовой).</summary>
    public static double PositiveAngleDelta(double from, double to)
    {
        var delta = to - from;
        while (delta < 0)
        {
            delta += Math.PI * 2.0;
        }

        while (delta >= Math.PI * 2.0)
        {
            delta -= Math.PI * 2.0;
        }

        return delta;
    }

    /// <summary>Точки мировой верхней дуги от fromWorld до toWorld через центр O.</summary>
    public static IReadOnlyList<CadPoint> TessellateUpperArc(
        CadPoint center,
        double radius,
        CadPoint fromWorld,
        CadPoint toWorld)
    {
        var angleFrom = GetAngleRad(center, fromWorld);
        var angleTo = GetAngleRad(center, toWorld);
        return CircularArc.Tessellate(center, radius, angleFrom, angleTo, FrameProfileGeometry.ArcTessellationRadians);
    }

    private static bool AngleInCounterClockwiseRange(double start, double end, double test)
    {
        var toTest = PositiveAngleDelta(start, test);
        var toEnd = PositiveAngleDelta(start, end);
        return toTest <= toEnd + CadMath.Epsilon;
    }

    public void ConvertToArc(double? defaultRadius = null)
    {
        if (SegmentType == SegmentType.Arc || ChordWidth <= CadMath.Epsilon)
        {
            return;
        }

        var targetRadius = ClampRadius(Math.Max(defaultRadius ?? DefaultInitRadiusMm, MinimumRadius));

        _isSyncing = true;
        SegmentType = SegmentType.Arc;
        _radius = targetRadius;
        _arcHeight = ComputeSagittaFromRadius(ChordWidth, _radius);
        _arcHeight = ClampArcHeight(_arcHeight);
        _radius = ClampRadius(ComputeRadiusFromSagitta(ChordWidth, _arcHeight));
        OnPropertyChanged(nameof(SegmentType));
        OnPropertyChanged(nameof(Radius));
        OnPropertyChanged(nameof(ArcHeight));
        OnPropertyChanged(nameof(IsLargeArc));
        _isSyncing = false;
    }

    public void ConvertToLine()
    {
        if (SegmentType == SegmentType.Line)
        {
            return;
        }

        _isSyncing = true;
        SegmentType = SegmentType.Line;
        _radius = 0;
        _arcHeight = 0;
        OnPropertyChanged(nameof(SegmentType));
        OnPropertyChanged(nameof(Radius));
        OnPropertyChanged(nameof(ArcHeight));
        OnPropertyChanged(nameof(ChordWidth));
        OnPropertyChanged(nameof(MinimumRadius));
        OnPropertyChanged(nameof(IsLargeArc));
        _isSyncing = false;
    }

    public ProfileSegmentSnapshot CaptureSnapshot() =>
        new()
        {
            SegmentType = SegmentType,
            StartX = StartX,
            StartY = StartY,
            EndX = EndX,
            EndY = EndY,
            Radius = _radius,
            ArcHeight = _arcHeight
        };

    public void ApplySnapshot(ProfileSegmentSnapshot snapshot)
    {
        _isSyncing = true;
        StartX = snapshot.StartX;
        StartY = snapshot.StartY;
        EndX = snapshot.EndX;
        EndY = snapshot.EndY;
        SegmentType = snapshot.SegmentType;
        _radius = snapshot.Radius;
        _arcHeight = snapshot.ArcHeight;
        OnPropertyChanged(nameof(StartX));
        OnPropertyChanged(nameof(StartY));
        OnPropertyChanged(nameof(EndX));
        OnPropertyChanged(nameof(EndY));
        OnPropertyChanged(nameof(SegmentType));
        OnPropertyChanged(nameof(Radius));
        OnPropertyChanged(nameof(ArcHeight));
        OnPropertyChanged(nameof(ChordWidth));
        OnPropertyChanged(nameof(MinimumRadius));
        OnPropertyChanged(nameof(IsLargeArc));
        _isSyncing = false;
    }
}

public sealed class ProfileSegmentSnapshot
{
    public SegmentType SegmentType { get; init; } = SegmentType.Line;
    public double StartX { get; init; }
    public double StartY { get; init; }     
    public double EndX { get; init; }
    public double EndY { get; init; }
    public double Radius { get; init; }
    public double ArcHeight { get; init; }
}
