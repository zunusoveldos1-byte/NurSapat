namespace NurSapat.Cad.Foundation.Primitives;

/// <summary>
/// Двумерный вектор ядра CAD (значимый тип, неизменяемый).
/// </summary>
public readonly record struct Vector2D(double X, double Y)
{
    public static Vector2D Zero { get; } = new(0, 0);
    public static Vector2D UnitX { get; } = new(1, 0);
    public static Vector2D UnitY { get; } = new(0, 1);

    /// <summary>Длина вектора.</summary>
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>Квадрат длины (без извлечения корня — для сравнений).</summary>
    public double LengthSquared => X * X + Y * Y;

    /// <summary>Нормализованный вектор. Для нулевого вектора возвращает <see cref="Zero"/>.</summary>
    public Vector2D Normalized()
    {
        var length = Length;
        return length <= double.Epsilon ? Zero : new Vector2D(X / length, Y / length);
    }

    /// <summary>Скалярное произведение.</summary>
    public double Dot(Vector2D other) => X * other.X + Y * other.Y;

    /// <summary>Векторное произведение (Z-компонента в 2D).</summary>
    public double Cross(Vector2D other) => X * other.Y - Y * other.X;

    /// <summary>Перпендикулярный вектор (поворот на +90°).</summary>
    public Vector2D Perpendicular() => new(-Y, X);

    public static Vector2D operator +(Vector2D left, Vector2D right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static Vector2D operator -(Vector2D left, Vector2D right) =>
        new(left.X - right.X, left.Y - right.Y);

    public static Vector2D operator -(Vector2D v) => new(-v.X, -v.Y);

    public static Vector2D operator *(Vector2D vector, double scalar) =>
        new(vector.X * scalar, vector.Y * scalar);

    public static Vector2D operator *(double scalar, Vector2D vector) => vector * scalar;

    public static Vector2D operator /(Vector2D vector, double scalar) =>
        new(vector.X / scalar, vector.Y / scalar);

    public override string ToString() => $"({X:0.######}; {Y:0.######})";
}
