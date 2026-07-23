using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Snapping;

/// <summary>
/// Тип точки привязки (snap kind).
/// </summary>
[Flags]
public enum SnapKind
{
    None = 0,
    Grid = 1,
    EndPoint = 2,
    MidPoint = 4,
    Center = 8,
    Intersection = 16,
    Perpendicular = 32,
    Tangent = 64,
    Nearest = 128,
    /// <summary>Все объектные типы привязки.</summary>
    Object = EndPoint | MidPoint | Center | Intersection | Perpendicular | Tangent
}

/// <summary>
/// Кандидат точки привязки: координаты, тип, расстояние до курсора, идентификатор источника.
/// </summary>
public readonly record struct SnapPoint(
    Point2D Position,
    SnapKind Kind,
    double Distance,
    Guid? SourceObjectId = null)
{
    /// <summary>Создать «нет результата».</summary>
    public static SnapPoint None(Point2D fallback) =>
        new(fallback, SnapKind.None, double.MaxValue, null);
}
