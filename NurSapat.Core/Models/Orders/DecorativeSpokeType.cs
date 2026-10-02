namespace NurSapat.Core.Models.Orders;

/// <summary>Тип декоративной раскладки (шпросы) на стеклопакете.</summary>
public enum DecorativeSpokeType
{
    /// <summary>Прямая сетка (горизонтальные и вертикальные линии).</summary>
    StandardGrid,

    /// <summary>Солнечные лучи из центра арки/круга.</summary>
    Sunburst,

    /// <summary>Внутренняя дуга, повторяющая арочный контур.</summary>
    ArchContour,

    /// <summary>Ромбовидная раскладка (диагонали и ромб по серединам сторон).</summary>
    Diamond
}
