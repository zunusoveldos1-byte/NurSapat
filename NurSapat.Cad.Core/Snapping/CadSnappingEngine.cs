using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Snapping;

public sealed class CadSnappingEngine
{
    public bool EnableGridSnap { get; set; } = true;

    public bool EnableObjectSnap { get; set; } = true;

    public double GridStep { get; set; } = 50;

    public double ToleranceWorld { get; set; } = 15;

    public SnapResult Snap(CadPoint input, IEnumerable<CadLine> snapLines)
    {
        var best = SnapResult.None(input);

        if (EnableGridSnap)
        {
            var grid = new CadPoint(
                Math.Round(input.X / GridStep) * GridStep,
                Math.Round(input.Y / GridStep) * GridStep);
            best = new SnapResult(grid, SnapKind.Grid, input.DistanceTo(grid));
        }

        if (!EnableObjectSnap)
        {
            return best;
        }

        foreach (var candidate in EnumerateObjectSnapPoints(snapLines))
        {
            var distance = input.DistanceTo(candidate.Point);
            if (distance <= ToleranceWorld && distance < best.Distance)
            {
                best = candidate with { Distance = distance };
            }
        }

        return best;
    }

    private static IEnumerable<SnapResult> EnumerateObjectSnapPoints(IEnumerable<CadLine> snapLines)
    {
        var lines = snapLines.ToArray();

        foreach (var line in lines)
        {
            yield return new SnapResult(line.Start, SnapKind.EndPoint, 0);
            yield return new SnapResult(line.End, SnapKind.EndPoint, 0);
            yield return new SnapResult(line.MidPoint, SnapKind.MidPoint, 0);
        }

        for (var i = 0; i < lines.Length; i++)
        {
            for (var j = i + 1; j < lines.Length; j++)
            {
                var intersection = CadMath.Intersect(lines[i], lines[j]);
                if (intersection is CadPoint point)
                {
                    yield return new SnapResult(point, SnapKind.Intersection, 0);
                }
            }
        }
    }
}
