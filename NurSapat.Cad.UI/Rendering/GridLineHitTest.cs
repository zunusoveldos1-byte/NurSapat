using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Единый HitTest по абсолютным GridLine: одна формула расстояния для X и Y.
/// Предпочитает локальный (узкий) узел колонки/ряда, а не сквозной parent.
/// </summary>
public static class GridLineHitTest
{
    public const double DefaultToleranceMm = 12.0;

    public static GridLine? HitTest(
        CadEditorViewModel editor,
        CadPoint worldPoint,
        double toleranceMm,
        GridLineOrientation? orientationFilter = null)
    {
        editor.RebuildGridLines();
        GridLine? best = null;
        var bestDistance = double.MaxValue;
        var bestLocalScore = double.MaxValue;
        var tolerance = Math.Max(toleranceMm, DefaultToleranceMm);

        foreach (var line in editor.GridLines.All)
        {
            if (orientationFilter is { } orientation && line.Orientation != orientation)
            {
                continue;
            }

            var distance = AxisMath.DistanceToLine(line, worldPoint.X, worldPoint.Y);
            if (distance > tolerance)
            {
                continue;
            }

            if (!IsWithinLineSpan(editor, line, worldPoint))
            {
                continue;
            }

            var localScore = ResolveLocalityScore(editor, line);
            // Ближе по перпендикуляру побеждает; при равной дистанции — более узкий (локальный) узел.
            if (distance > bestDistance + CadMath.MmPrecision)
            {
                continue;
            }

            if (Math.Abs(distance - bestDistance) <= CadMath.MmPrecision
                && localScore >= bestLocalScore)
            {
                continue;
            }

            bestDistance = distance;
            bestLocalScore = localScore;
            best = line;
        }

        return best;
    }

    public static ImpostTarget? ToImpostTarget(CadEditorViewModel editor, GridLine line)
    {
        if (line.SourceNodeId is not Guid nodeId || editor.FrameRoot is null)
        {
            return null;
        }

        var node = editor.FrameRoot.FindNodeById(nodeId);
        if (node is null)
        {
            return null;
        }

        return new ImpostTarget
        {
            SplitNode = node,
            UniformGapIndex = line.UniformGapIndex,
            GridLineId = line.Id,
            Orientation = line.Orientation
        };
    }

    /// <summary>
    /// Пролёт самой GridLine (колонка/ряд источника), а не всего контейнера.
    /// </summary>
    private static bool IsWithinLineSpan(CadEditorViewModel editor, GridLine line, CadPoint worldPoint)
    {
        var (spanMin, spanMax) = GridLineSpanResolver.Resolve(editor, line);
        if (spanMax - spanMin <= CadMath.Epsilon && editor.FrameRoot is { } root)
        {
            (spanMin, spanMax) = AxisMath.ContainerSpan(
                line.Orientation,
                root.X,
                root.Y,
                root.Width,
                root.Height);
        }

        var alongSpan = AxisMath.AlongSpan(line.Orientation, worldPoint.X, worldPoint.Y);
        return alongSpan >= spanMin - CadMath.MmPrecision && alongSpan <= spanMax + CadMath.MmPrecision;
    }

    /// <summary>
    /// Меньше = более локальный узел (узкая колонка/ряд).
    /// </summary>
    private static double ResolveLocalityScore(CadEditorViewModel editor, GridLine line)
    {
        if (line.SourceNodeId is not Guid nodeId || editor.FrameRoot is null)
        {
            return double.MaxValue;
        }

        var node = editor.FrameRoot.FindNodeById(nodeId);
        if (node is null)
        {
            return double.MaxValue;
        }

        return line.Orientation == GridLineOrientation.Horizontal
            ? node.Width
            : node.Height;
    }
}
