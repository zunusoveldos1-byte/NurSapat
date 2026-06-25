using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Snapping;

/// <summary>
/// Провайдер точек привязки: даёт кандидатов по области запроса.
/// </summary>
/// <remarks>
/// Обобщённый аналог существующего <c>NurSapat.Cad.Core.ISnapProvider</c>.
/// Конкретные провайдеры (Grid, Object, Extension) регистрируются в <see cref="ISnapManager"/>.
/// </remarks>
public interface ISnapProvider
{
    /// <summary>Получить точки привязки в заданной мировой области.</summary>
    IEnumerable<SnapPoint> GetCandidates(SnapKind kinds, Bounds2D worldRegion);
}
