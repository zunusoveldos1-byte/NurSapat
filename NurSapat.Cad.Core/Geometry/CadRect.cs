namespace NurSapat.Cad.Core.Geometry;

public readonly record struct CadRect(double X, double Y, double Width, double Height)
{
    public CadPoint TopLeft => new(X, Y);

    public CadPoint BottomRight => new(X + Width, Y - Height);

    public bool Contains(CadPoint point)
    {
        var minX = Math.Min(X, X + Width);
        var maxX = Math.Max(X, X + Width);
        var minY = Math.Min(Y, Y - Height);
        var maxY = Math.Max(Y, Y - Height);
        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
    }

    public static CadRect FromPoints(CadPoint a, CadPoint b)
    {
        var minX = Math.Min(a.X, b.X);
        var maxX = Math.Max(a.X, b.X);
        var minY = Math.Min(a.Y, b.Y);
        var maxY = Math.Max(a.Y, b.Y);
        return new CadRect(minX, maxY, maxX - minX, maxY - minY);
    }
}
