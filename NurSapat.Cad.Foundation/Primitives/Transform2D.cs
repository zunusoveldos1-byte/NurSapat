using System.Globalization;

namespace NurSapat.Cad.Foundation.Primitives;

/// <summary>
/// Неизменяемое аффинное преобразование 2D: перенос + поворот + масштаб.
/// </summary>
/// <remarks>
/// Применяется к точкам через <see cref="Apply"/>. Инвариант идентичности — <see cref="Identity"/>.
/// Порядок применения: сначала масштаб, затем поворот, затем перенос (S * R * T).
/// </remarks>
public readonly record struct Transform2D(Vector2D Translation, double Rotation, Vector2D Scale)
{
    /// <summary>Тождественное преобразование (без изменений).</summary>
    public static Transform2D Identity { get; } = new(Vector2D.Zero, 0, Vector2D.UnitX + Vector2D.UnitY);

    /// <summary>Признак тождественности.</summary>
    public bool IsIdentity =>
        Translation == Vector2D.Zero && Rotation == 0 &&
        Scale == (Vector2D.UnitX + Vector2D.UnitY);

    /// <summary>Создать только перенос.</summary>
    public static Transform2D Translate(Vector2D offset) => new(offset, 0, Vector2D.UnitX + Vector2D.UnitY);

    /// <summary>Создать только поворот (в радианах).</summary>
    public static Transform2D Rotate(double radians) =>
        new(Vector2D.Zero, radians, Vector2D.UnitX + Vector2D.UnitY);

    /// <summary>Создать только масштаб.</summary>
    public static Transform2D ScaleUniform(double factor) =>
        new(Vector2D.Zero, 0, new Vector2D(factor, factor));

    /// <summary>Применить преобразование к точке.</summary>
    public Point2D Apply(Point2D point)
    {
        var scaled = new Point2D(point.X * Scale.X, point.Y * Scale.Y);
        if (Rotation != 0)
        {
            var cos = Math.Cos(Rotation);
            var sin = Math.Sin(Rotation);
            scaled = new Point2D(scaled.X * cos - scaled.Y * sin, scaled.X * sin + scaled.Y * cos);
        }
        return scaled + Translation;
    }

    /// <summary>Применить преобразование к вектору (перенос игнорируется).</summary>
    public Vector2D ApplyVector(Vector2D vector)
    {
        var scaled = new Vector2D(vector.X * Scale.X, vector.Y * Scale.Y);
        if (Rotation == 0) return scaled;
        var cos = Math.Cos(Rotation);
        var sin = Math.Sin(Rotation);
        return new Vector2D(scaled.X * cos - scaled.Y * sin, scaled.X * sin + scaled.Y * cos);
    }

    /// <summary>Обратное преобразование.</summary>
    public Transform2D Inverse()
    {
        var invScale = Scale.X != 0 && Scale.Y != 0
            ? new Vector2D(1.0 / Scale.X, 1.0 / Scale.Y)
            : Vector2D.UnitX + Vector2D.UnitY;
        var invRotation = -Rotation;

        // Обратный перенос: сначала убираем поворот/масштаб с Translation
        var t = new Transform2D(Vector2D.Zero, invRotation, invScale);
        var invTranslation = -t.ApplyVector(Translation);
        return new Transform2D(invTranslation, invRotation, invScale);
    }

    /// <summary>Композиция: сначала <paramref name="other"/>, затем this.</summary>
    public Transform2D Then(Transform2D other) => new(
        Translation + other.Translation,
        Rotation + other.Rotation,
        new Vector2D(Scale.X * other.Scale.X, Scale.Y * other.Scale.Y));

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture,
            "T({0:0.###}, {1:0.###}) R({2:0.###}) S({3:0.###}, {4:0.###})",
            Translation.X, Translation.Y, Rotation, Scale.X, Scale.Y);
}
