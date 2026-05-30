namespace NurSapat.Cad.Core.Geometry;

public sealed class CadPolygon
{
    public IReadOnlyList<CadPoint> Points { get; }

    public CadPolygon(IEnumerable<CadPoint> points)
    {
        Points = points.ToArray();
        if (Points.Count < 3)
        {
            throw new ArgumentException("Polygon requires at least three points.", nameof(points));
        }
    }

    public IEnumerable<CadLine> Edges()
    {
        for (var i = 0; i < Points.Count; i++)
        {
            yield return new CadLine(Points[i], Points[(i + 1) % Points.Count]);
        }
    }

    public CadRect Bounds()
    {
        var minX = Points.Min(p => p.X);
        var maxX = Points.Max(p => p.X);
        var minY = Points.Min(p => p.Y);
        var maxY = Points.Max(p => p.Y);
        return new CadRect(minX, maxY, maxX - minX, maxY - minY);
    }
}
