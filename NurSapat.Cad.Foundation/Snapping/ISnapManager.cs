using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Snapping;

/// <summary>
/// Менеджер привязок: агрегирует провайдеров, находит лучший snap-point.
/// </summary>
/// <remarks>
/// Обобщённый аналог существующего <c>CadSnappingEngine</c>.
/// На этом этапе — скелет (без реализации алгоритмов); алгоритмы на Этапе 4 (Tools).
/// Существующий <c>CadSnappingEngine</c> остаётся в Cad.Core нетронутым.
/// </remarks>
public interface ISnapManager
{
    SnapSettings Settings { get; }
    void RegisterProvider(ISnapProvider provider);
    void UnregisterProvider(ISnapProvider provider);
    SnapPoint Snap(Point2D inputPoint, SnapKind kinds);
}
