namespace NurSapat.Cad.Foundation.Primitives;

/// <summary>
/// Прямоугольные границы в мировом пространстве (AABB — Axis-Aligned Bounding Box).
/// Хранятся как мин/макс по осям — корректно даже для «перевёрнутых» прямоугольников.
/// </summary>
public readonly record struct Bounds2D(double MinX, double MinY, double MaxX, double MaxY)
{
    public static Bounds2D Empty { get; } = new(0, 0, 0, 0);

    public static Bounds2D Zero { get; } = new(0, 0, 0, 0);

    /// <summary>Ширина (всегда неотрицательная).</summary>
    public double Width => Math.Max(0, MaxX - MinX);

    /// <summary>Высота (всегда неотрицательная).</summary>
    public double Height => Math.Max(0, MaxY - MinY);

    /// <summary>Центр прямоугольника.</summary>
    public Point2D Center => new((MinX + MaxX) * 0.5, (MinY + MaxY) * 0.5);

    /// <summary>Площадь (0 для вырожденного прямоугольника).</summary>
    public double Area => Width * Height;

    /// <summary>Признак вырожденности (нулевая площадь).</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>Создать границы по двум точкам (любые противоположные углы).</summary>
    public static Bounds2D FromPoints(Point2D a, Point2D b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
            Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    /// <summary>Создать границы по центру и размеру.</summary>
    public static Bounds2D FromCenter(Point2D center, double width, double height)
    {
        var hw = width * 0.5;
        var hh = height * 0.5;
        return new Bounds2D(center.X - hw, center.Y - hh, center.X + hw, center.Y + hh);
    }

    /// <summary>Содержит ли точка.</summary>
    public bool Contains(Point2D point) =>
        point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;

    /// <summary>Пересекается ли с другими границами.</summary>
    public bool Intersects(Bounds2D other) =>
        MinX <= other.MaxX && MaxX >= other.MinX &&
        MinY <= other.MaxY && MaxY >= other.MinY;

    /// <summary>Объединение с другими границами.</summary>
    public Bounds2D Union(Bounds2D other) =>
        new(Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY));

    /// <summary>Расширить границы так, чтобы包含ить точку.</summary>
    public Bounds2D Include(Point2D point) =>
        new(Math.Min(MinX, point.X), Math.Min(MinY, point.Y),
            Math.Max(MaxX, point.X), Math.Max(MaxY, point.Y));

    /// <summary>Расширить границы на заданный отступ со всех сторон.</summary>
    public Bounds2D Inflate(double dx, double dy) =>
        new(MinX - dx, MinY - dy, MaxX + dx, MaxY + dy);

    public override string ToString() =>
        $"[{MinX:0.###}, {MinY:0.###}] → [{MaxX:0.###}, {MaxY:0.###}] ({Width:0.###}×{Height:0.###})";
}
