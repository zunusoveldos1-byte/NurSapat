using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Размерные опоры внешней цепи: края контейнера + оси импостов (Center Line).
/// Грани толщины профиля в цепочку не входят — сегменты Outer→Axis→Outer.
/// </summary>
internal static class DimensionChainFromGridLines
{
    public static bool TryCollectSegments(
        CadEditorViewModel editor,
        FrameNode root,
        GridLineOrientation orientation,
        out IReadOnlyList<double> segmentSizesMm,
        out IReadOnlyList<double> boundaryWorld)
    {
        segmentSizesMm = Array.Empty<double>();
        boundaryWorld = Array.Empty<double>();

        editor.RebuildGridLines();
        var grid = editor.GridLines;
        var alongX = orientation == GridLineOrientation.Vertical;

        var (axisMin, axisMax) = AxisMath.ContainerAxisRange(
            orientation,
            root.X,
            root.Y,
            root.Width,
            root.Height);

        var raw = new List<double> { CadMath.RoundMm(axisMin) };

        foreach (var line in grid.OfOrientation(orientation))
        {
            if (TryResolveAttachedTransitionFaces(editor, root, line, out var face1, out var face2))
            {
                // Переходной профиль: по-прежнему грани (толщина не даёт отдельный размер).
                AddInterior(raw, face1, axisMin, axisMax);
                AddInterior(raw, face2, axisMin, axisMax);
            }
            else
            {
                // Импост / виртуальная линия: засечка на оси (Center Line), не на InnerEdge.
                AddInterior(raw, line.Coordinate, axisMin, axisMax);
            }
        }

        raw.Add(CadMath.RoundMm(axisMax));
        DeduplicateSorted(raw);

        if (raw.Count < 2)
        {
            return false;
        }

        return GeometryElementRendererDimensionBridge.TryBuildOpeningSegments(
            editor,
            root,
            raw,
            alongX,
            out segmentSizesMm,
            out boundaryWorld);
    }

    private static bool TryResolveAttachedTransitionFaces(
        CadEditorViewModel editor,
        FrameNode root,
        GridLine line,
        out double face1,
        out double face2)
    {
        face1 = face2 = 0;
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (element.HostGridLineId is Guid hostId && hostId == line.Id.Value)
            {
                var thickness = TransitionAdjacentFrameGeometry.ResolveTransitionProfileThickness(element);
                var bounds = TransitionProfileGeometry.ResolveTransitionProfileBounds(line, thickness, root);
                face1 = bounds.Face1;
                face2 = bounds.Face2;
                return true;
            }

            // Fallback: центр элемента совпадает с осью GridLine.
            var center = line.Orientation == GridLineOrientation.Vertical
                ? (double)element.X
                : (double)element.Y;
            if (!AxisMath.NearlyEqualCoordinate(center, line.Coordinate))
            {
                continue;
            }

            var t = TransitionAdjacentFrameGeometry.ResolveTransitionProfileThickness(element);
            var b = TransitionProfileGeometry.ResolveTransitionProfileBounds(line, t, root);
            face1 = b.Face1;
            face2 = b.Face2;
            return true;
        }

        return false;
    }

    private static void AddInterior(List<double> values, double value, double outerMin, double outerMax)
    {
        var rounded = CadMath.RoundMm(value);
        if (rounded <= outerMin + CadMath.MmPrecision || rounded >= outerMax - CadMath.MmPrecision)
        {
            return;
        }

        values.Add(rounded);
    }

    private static void DeduplicateSorted(List<double> values)
    {
        values.Sort();
        for (var i = values.Count - 1; i > 0; i--)
        {
            if (Math.Abs(values[i] - values[i - 1]) <= CadMath.MmPrecision)
            {
                values.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// Мост к существующему построителю сегментов в GeometryElementRenderer (internal).
/// </summary>
internal static class GeometryElementRendererDimensionBridge
{
    public static bool TryBuildOpeningSegments(
        CadEditorViewModel editor,
        FrameNode root,
        List<double> rawBoundaries,
        bool alongX,
        out IReadOnlyList<double> segmentSizesMm,
        out IReadOnlyList<double> boundaryWorld) =>
        GeometryElementRenderer.TryBuildOpeningSegmentsForGrid(
            editor,
            root,
            rawBoundaries,
            alongX,
            out segmentSizesMm,
            out boundaryWorld);
}
