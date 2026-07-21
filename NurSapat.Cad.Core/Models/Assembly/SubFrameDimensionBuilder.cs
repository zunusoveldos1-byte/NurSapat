using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Результат построения размерной цепочки подрамы.
/// </summary>
public sealed class SubFrameDimensionResult
{
    public double OuterWidthMm { get; init; }
    public double InternalWidthMm { get; init; }
    public IReadOnlyList<double> SegmentSizesMm { get; init; } = [];
    public IReadOnlyList<double> BoundaryWorldX { get; init; } = [];
    public double OuterHeightMm { get; init; }
    public double InternalHeightMm { get; init; }
    public IReadOnlyList<double> SegmentHeightsMm { get; init; } = [];
    public IReadOnlyList<double> BoundaryWorldY { get; init; } = [];
}

/// <summary>
/// Три слоя размеров подрамы: outer (display), internal math, segment chain.
/// </summary>
public static class SubFrameDimensionBuilder
{
    /// <summary>
    /// Делит внутреннюю ширину на равные сегменты (осевые).
    /// Пример: internal 1396, 2 сегмента → 698 + 698; outer остаётся 1400.
    /// </summary>
    public static SubFrameDimensionResult BuildHorizontalChain(
        SubFrame subFrame,
        IReadOnlyList<double> internalAxisLocalX,
        double toleranceMm = CadMath.MmPrecision)
    {
        var outer = subFrame.OuterWidth;
        var internalWidth = subFrame.InternalWidth;

        var boundaries = new List<double> { 0 };
        foreach (var axis in internalAxisLocalX.OrderBy(v => v))
        {
            var rounded = CadMath.RoundMm(axis);
            if (boundaries.Count == 0 || Math.Abs(boundaries[^1] - rounded) > toleranceMm)
            {
                boundaries.Add(rounded);
            }
        }

        boundaries.Add(internalWidth);

        var segments = new List<double>();
        var boundaryWorld = new List<double> { subFrame.OriginWorldX };

        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var size = CadMath.RoundMm(boundaries[i + 1] - boundaries[i]);
            if (size > CadMath.Epsilon)
            {
                segments.Add(size);
                boundaryWorld.Add(CadMath.RoundMm(subFrame.OriginWorldX + boundaries[i + 1]));
            }
        }

        if (boundaryWorld.Count != segments.Count + 1 && segments.Count > 0)
        {
            boundaryWorld = [subFrame.OriginWorldX, .. boundaryWorld.Skip(1)];
        }

        return new SubFrameDimensionResult
        {
            OuterWidthMm = outer,
            InternalWidthMm = internalWidth,
            SegmentSizesMm = segments,
            BoundaryWorldX = boundaryWorld
        };
    }

    public static SubFrameDimensionResult BuildVerticalChain(
        SubFrame subFrame,
        IReadOnlyList<double> internalAxisLocalY,
        double toleranceMm = CadMath.MmPrecision)
    {
        var boundaries = new List<double> { 0 };
        foreach (var axis in internalAxisLocalY.OrderBy(value => value))
        {
            var rounded = CadMath.RoundMm(axis);
            if (Math.Abs(boundaries[^1] - rounded) > toleranceMm)
            {
                boundaries.Add(rounded);
            }
        }

        boundaries.Add(subFrame.InternalHeight);
        var segments = new List<double>();
        var world = new List<double> { subFrame.OriginWorldY };
        for (var index = 0; index < boundaries.Count - 1; index++)
        {
            var size = CadMath.RoundMm(boundaries[index + 1] - boundaries[index]);
            if (size <= CadMath.Epsilon)
            {
                continue;
            }

            segments.Add(size);
            world.Add(subFrame.LocalToWorldY(boundaries[index + 1]));
        }

        return new SubFrameDimensionResult
        {
            OuterHeightMm = subFrame.OuterHeight,
            InternalHeightMm = subFrame.InternalHeight,
            SegmentHeightsMm = segments,
            BoundaryWorldY = world
        };
    }

    /// <summary>
    /// Равномерное деление внутренней ширины на N частей.
    /// </summary>
    public static IReadOnlyList<double> SplitInternalWidthEvenly(double internalWidth, int partCount)
    {
        if (partCount < 1 || internalWidth <= CadMath.Epsilon)
        {
            return [];
        }

        var partSize = CadMath.RoundMm(internalWidth / partCount);
        var segments = new List<double>();
        var used = 0.0;
        for (var i = 0; i < partCount; i++)
        {
            var isLast = i == partCount - 1;
            var size = isLast
                ? CadMath.RoundMm(internalWidth - used)
                : partSize;
            segments.Add(size);
            used += size;
        }

        return segments;
    }

    public static bool SegmentsSumToInternal(
        IReadOnlyList<double> segments,
        double internalWidth,
        double toleranceMm = CadMath.MmPrecision) =>
        Math.Abs(segments.Sum() - internalWidth) <= toleranceMm;
}
