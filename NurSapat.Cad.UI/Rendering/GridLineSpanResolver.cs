using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Единый расчёт пролёта профиля вдоль GridLine.
/// На этапе E делегирует в существующие V/H-резолверы через одну точку входа —
/// потребители больше не ветвятся по ориентации.
/// </summary>
public static class GridLineSpanResolver
{
    public static (double SpanMin, double SpanMax) Resolve(
        CadEditorViewModel editor,
        GridLine line)
    {
        var root = editor.FrameRoot;
        if (root is null || line.SourceNodeId is not Guid nodeId)
        {
            return AxisMath.ContainerSpan(line.Orientation, 0, 0, 0, 0);
        }

        var node = root.FindNodeById(nodeId);
        if (node is null)
        {
            return AxisMath.ContainerSpan(line.Orientation, root.X, root.Y, root.Width, root.Height);
        }

        if (line.Orientation == GridLineOrientation.Vertical)
        {
            var (bottom, top) = FrameNodeRenderer.ResolveFictitiousVerticalImpostSpan(
                editor,
                node,
                line.Coordinate);
            return (bottom, top);
        }

        var (left, right) = HorizontalImpostGeometry.ResolveSpanAtY(editor, node, line.Coordinate);
        return (left, right);
    }

    /// <summary>
    /// Абсолютные грани соседнего TransitionFrame, примыкающего к линии.
    /// </summary>
    public static bool TryGetAdjacentTransitionFaces(
        CadEditorViewModel editor,
        GridLine line,
        out double face1,
        out double face2)
    {
        face1 = face2 = 0;
        var root = editor.FrameRoot;
        if (root is null)
        {
            return false;
        }

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
        }

        return false;
    }
}
