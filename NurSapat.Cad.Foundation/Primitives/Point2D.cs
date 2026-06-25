namespace NurSapat.Cad.Foundation.Primitives;

/// <summary>
/// Точка в двумерном пространстве ядра CAD (значимый тип, неизменяемый).
/// </summary>
/// <remarks>
/// Это фундаментальный тип ядра Foundation, а не дубликат <c>CadPoint</c>.
/// <c>CadPoint</c> относится к CAD-геометрии (Этап 2) и останется в NurSapat.Cad.Core.
/// </remarks>
public readonly record struct Point2D(double X, double Y)
{
    /// <summary>Начало координат (0;0).</summary>
    public static Point2D Zero { get; } = new(0, 0);

    /// <summary>Расстояние до другой точки.</summary>
    public double DistanceTo(Point2D other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Проверка близости к другой точке в пределах допуска.</summary>
    public bool IsNear(Point2D other, double tolerance) =>
        Math.Abs(X - other.X) < tolerance && Math.Abs(Y - other.Y) < tolerance;

    /// <summary>Округление координат.</summary>
    public Point2D Round(int digits = 6) => new(Math.Round(X, digits), Math.Round(Y, digits));

    public static Point2D operator +(Point2D point, Vector2D vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    public static Point2D operator -(Point2D point, Vector2D vector) =>
        new(point.X - vector.X, point.Y - vector.Y);

    public static Vector2D operator -(Point2D left, Point2D right) =>
        new(left.X - right.X, left.Y - right.Y);

    public override string ToString() => $"({X:0.######}; {Y:0.######})";
}
