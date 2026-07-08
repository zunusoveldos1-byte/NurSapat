using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models.Construction;

/// <summary>
/// Материал конструкции для расчёта физического объёма и спецификации.
/// </summary>
public sealed class ConstructionMaterial
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "";

    /// <summary>Инструмент / тип элемента, к которому применим материал.</summary>
    public GeometryElementKind ApplicableKind { get; init; }

    /// <summary>Лицевая ширина профиля, мм.</summary>
    public double ProfileWidth { get; init; }

    /// <summary>Площадь сечения профиля, мм² (для расчёта объёма).</summary>
    public double CrossSectionArea { get; init; }

    public EndCutType EndCutType { get; init; }
}
