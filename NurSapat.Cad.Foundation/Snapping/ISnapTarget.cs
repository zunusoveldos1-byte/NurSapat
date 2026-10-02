using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Snapping;

/// <summary>
/// Объект, способный предоставлять точки привязки (snap candidates).
/// </summary>
/// <remarks>
/// SceneObject будет реализовывать этот интерфейс (Этап 2).
/// Snap-менеджер собирает кандидатов от всех видимых объектов в видимой области.
/// </remarks>
public interface ISnapTarget
{
    /// <summary>Получить точки привязки для данного типа (kind) в заданной области.</summary>
    IEnumerable<SnapPoint> GetSnapCandidates(SnapKind kinds, Bounds2D worldRegion);
}
