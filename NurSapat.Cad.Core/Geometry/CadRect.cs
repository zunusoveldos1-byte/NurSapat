namespace NurSapat.Cad.Core.Geometry;

public readonly record struct CadRect(double X, double Y, double Width, double Height)
{
    public CadPoint TopLeft => new(X, Y);
    public CadPoint TopRight => new(X + Width, Y);
    public CadPoint BottomLeft => new(X, Y - Height);

    public CadPoint BottomRight => new(X + Width, Y - Height);

    public bool Contains(CadPoint point)
    {
        var minX = Math.Min(X, X + Width);
        var maxX = Math.Max(X, X + Width);
        var minY = Math.Min(Y, Y - Height);
        var maxY = Math.Max(Y, Y - Height);
        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
    }

    public bool Intersects(CadRect other)
    {
        var left = Math.Min(X, X + Width);
        var right = Math.Max(X, X + Width);
        var top = Math.Max(Y, Y - Height);
        var bottom = Math.Min(Y, Y - Height);
        var otherLeft = Math.Min(other.X, other.X + other.Width);
        var otherRight = Math.Max(other.X, other.X + other.Width);
        var otherTop = Math.Max(other.Y, other.Y - other.Height);
        var otherBottom = Math.Min(other.Y, other.Y - other.Height);
        return left <= otherRight && right >= otherLeft && bottom <= otherTop && top >= otherBottom;
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
