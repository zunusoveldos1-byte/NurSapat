using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Обёртка над GridLineCollection для одной подрамы.
/// </summary>
public sealed class SubFrameGrid
{
    private GridLineCollection _lines = GridLineCollection.Empty;

    public SubFrame Owner { get; }

    public GridLineCollection Lines => _lines;

    public SubFrameGrid(SubFrame owner)
    {
        Owner = owner;
        Rebuild();
    }

    public void Rebuild()
    {
        var projected = FrameNodeGridLineProjector.Project(Owner.Root);
        _lines = OffsetToWorld(projected, Owner.OriginWorldX, Owner.OriginWorldY);
    }

    private static GridLineCollection OffsetToWorld(
        GridLineCollection local,
        double originX,
        double originY)
    {
        if (local.All.Count == 0)
        {
            return GridLineCollection.Empty;
        }

        var shifted = new List<GridLine>(local.All.Count);
        foreach (var line in local.All)
        {
            var coordinate = line.Orientation == GridLineOrientation.Vertical
                ? CadMath.RoundMm(line.Coordinate + originX)
                : CadMath.RoundMm(line.Coordinate + originY);
            shifted.Add(new GridLine
            {
                Id = line.Id,
                Orientation = line.Orientation,
                Coordinate = coordinate,
                Kind = line.Kind,
                ThicknessMm = line.ThicknessMm,
                SourceNodeId = line.SourceNodeId,
                UniformGapIndex = line.UniformGapIndex
            });
        }

        return new GridLineCollection(shifted);
    }
}
