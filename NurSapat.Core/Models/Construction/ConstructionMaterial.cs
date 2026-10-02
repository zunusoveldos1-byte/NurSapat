using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models.Construction;

/// <summary>
/// Материал конструкции для расчёта физического объёма и спецификации.
/// </summary>
public sealed class ConstructionMaterial
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "";

    /// <summary>Артикул материала каталога (стабильный ключ для PostgreSQL).</summary>
    public string CatalogArticle { get; init; } = "";

    /// <summary>Инструмент / тип элемента, к которому применим материал.</summary>
    public GeometryElementKind ApplicableKind { get; init; }

    /// <summary>Лицевая ширина профиля, мм.</summary>
    public double ProfileWidth { get; init; }

    /// <summary>Площадь сечения профиля, мм² (для расчёта объёма).</summary>
    public double CrossSectionArea { get; init; }

    public EndCutType EndCutType { get; init; }

    /// <summary>
    /// Переопределение толщины отрисовки на чертеже, мм.
    /// null — рисовать по реальной толщине профиля.
    /// Используется тонкими накладными профилями (нащельник и т.п.).
    /// </summary>
    public double? DrawThicknessMm { get; init; }

    /// <summary>
    /// Материал ведёт себя как «умный монтажный» профиль: при наведении
    /// автоматически определяет ориентацию (стойка/ригель) и магнитно
    /// прилипает к ближайшей оси конструкции (нащельник).
    /// </summary>
    public bool SnapsToConstructionAxes { get; init; }

    /// <summary>
    /// Сколько мм забирает профиль от светового проёма на примыкающем крае ячейки.
    /// Для нащельника — 4 мм; null — не влияет на расчёт проёмов.
    /// </summary>
    public double? LightOpeningInsetMm { get; init; }

    /// <summary>
    /// Дверной профиль: при размещении игнорирует сквозные горизонтальные оси
    /// разметки и растягивается на весь вертикальный пролёт конструкции
    /// (от низа до верха рамы). Пример — «65 компланарная Т (для двери)».
    /// </summary>
    public bool SpansFullConstructionHeight { get; init; }
}
