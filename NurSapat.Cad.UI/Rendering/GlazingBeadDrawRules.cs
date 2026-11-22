using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Правила видимости штапика на остеклении.
/// При <see cref="HasBead"/> заливка стекла ужимается во внутренний контур штапика;
/// сам профиль рисует <see cref="GlazingBeadRenderer"/>.
/// </summary>
internal static class GlazingBeadDrawRules
{
    /// <summary>
    /// Есть ли штапик у элемента остекления / проёма.
    /// Учитывает явный флаг <see cref="GeometryElement.HasBead"/> и связи ParentBeadId / Bead.
    /// </summary>
    public static bool HasBead(GeometryElement? element, IReadOnlyList<GeometryElement>? allElements = null)
    {
        if (element is null)
        {
            return false;
        }

        // Явный флаг — основной переключатель для будущего инструмента вкл/выкл.
        if (element.HasBead)
        {
            return true;
        }

        if (element.ParentBeadId is not null)
        {
            return true;
        }

        // Элемент штапика на холсте: по умолчанию «со штапиком», пока флаг не сброшен явно.
        // (HasBead=false на Bead используется для предпросмотра стыков 45°.)
        if (element.Kind == GeometryElementKind.Bead)
        {
            return true;
        }

        if (allElements is null)
        {
            return false;
        }

        if (element.Kind == GeometryElementKind.Glazing)
        {
            return allElements.Any(item =>
                item.Kind == GeometryElementKind.Bead
                && (item.LinkedGlazingId == element.Id || item.ParentGlazingId == element.Id));
        }

        if (element.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash)
        {
            return allElements.Any(item =>
                item.Kind == GeometryElementKind.Bead
                && item.HostSashId == element.Id);
        }

        return false;
    }

    /// <summary>
    /// Рисовать ли диагональные стыки 45° на внутреннем контуре остекления/штапика.
    /// Со штапиком — нет (профиль штапика отдельным слоем); без штапика — да.
    /// </summary>
    public static bool ShouldDrawCornerMiters(
        GeometryElement? element,
        IReadOnlyList<GeometryElement>? allElements = null) =>
        !HasBead(element, allElements);
}
