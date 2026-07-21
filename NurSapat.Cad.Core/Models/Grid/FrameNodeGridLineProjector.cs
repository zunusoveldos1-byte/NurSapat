using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>
/// Проецирует дерево FrameNode в единую коллекцию абсолютных GridLine.
/// Ветвление Vertical/Horizontal допустимо только здесь — потребители читают GridLine.
/// </summary>
public static class FrameNodeGridLineProjector
{
    public static GridLineCollection Project(FrameNode? root)
    {
        if (root is null)
        {
            return GridLineCollection.Empty;
        }

        var lines = new List<GridLine>();
        foreach (var node in root.EnumerateInternalNodes())
        {
            CollectFromNode(node, lines);
        }

        return new GridLineCollection(lines);
    }

    private static void CollectFromNode(FrameNode node, List<GridLine> lines)
    {
        if (node.IsUniformSplit)
        {
            CollectUniform(node, lines);
            return;
        }

        if (node.SplitOrientation == SplitOrientation.Vertical
            && node.TryGetVerticalImpostAxisWorldX(out var axisX))
        {
            lines.Add(CreateLine(
                GridLineOrientation.Vertical,
                axisX,
                node,
                uniformGapIndex: null));
        }
        else if (node.SplitOrientation == SplitOrientation.Horizontal
                 && node.TryGetHorizontalImpostAxisWorldY(out var axisY))
        {
            lines.Add(CreateLine(
                GridLineOrientation.Horizontal,
                axisY,
                node,
                uniformGapIndex: null));
        }
    }

    private static void CollectUniform(FrameNode node, List<GridLine> lines)
    {
        var orientation = node.SplitOrientation switch
        {
            SplitOrientation.Vertical => GridLineOrientation.Vertical,
            SplitOrientation.Horizontal => GridLineOrientation.Horizontal,
            _ => (GridLineOrientation?)null
        };
        if (orientation is null)
        {
            return;
        }

        var splitOrientation = node.SplitOrientation;
        for (var gap = 0; gap < node.UniformCells.Count - 1; gap++)
        {
            if (!node.TryGetUniformImpostAxisWorld(splitOrientation, gap, out var axis))
            {
                continue;
            }

            lines.Add(CreateLine(orientation.Value, axis, node, gap));
        }
    }

    private static GridLine CreateLine(
        GridLineOrientation orientation,
        double coordinate,
        FrameNode node,
        int? uniformGapIndex)
    {
        var thickness = node.GetResolvedImpostThicknessMm();
        var kind = thickness <= CadMath.Epsilon || node.LineStyle == ImpostLineStyle.Dashed
            ? GridLineKind.Virtual
            : GridLineKind.Physical;

        return new GridLine
        {
            Id = StableId(node.NodeId, orientation, uniformGapIndex),
            Orientation = orientation,
            Coordinate = CadMath.RoundMm(coordinate),
            Kind = kind,
            ThicknessMm = kind == GridLineKind.Virtual ? 0 : CadMath.RoundMm(thickness),
            SourceNodeId = node.NodeId,
            UniformGapIndex = uniformGapIndex
        };
    }

    /// <summary>
    /// Детерминированный Id по NodeId + orientation + gap, чтобы ссылки переживали репроекцию.
    /// </summary>
    private static GridLineId StableId(Guid nodeId, GridLineOrientation orientation, int? gapIndex)
    {
        var gap = gapIndex ?? -1;
        var bytes = nodeId.ToByteArray();
        bytes[0] ^= (byte)orientation;
        bytes[1] ^= (byte)(gap & 0xFF);
        bytes[2] ^= (byte)((gap >> 8) & 0xFF);
        return new GridLineId(new Guid(bytes));
    }
}
