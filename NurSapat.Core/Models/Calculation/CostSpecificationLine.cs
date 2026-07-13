namespace NurSapat.Core.Models.Calculation;

using NurSapat.Core.Validation;

public sealed class CostSpecificationLine
{
    /// <summary>Идентификатор материала каталога (обязательная ссылка для расчёта и PostgreSQL).</summary>
    public Guid MaterialId { get; init; }

    /// <summary>Идентификатор материала конструкции (CAD), если строка получена из геометрии.</summary>
    public Guid? ConstructionMaterialId { get; init; }

    public CostMaterialCategory Category { get; init; }

    /// <summary>Денормализованное наименование для отображения и экспорта.</summary>
    public string MaterialName { get; init; } = string.Empty;

    /// <summary>Денормализованный артикул для отображения и экспорта.</summary>
    public string Article { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public string Unit { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public decimal WastePercent { get; init; }

    public decimal LineTotal =>
        decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    public void Validate()
    {
        DomainInvariants.RequireNonEmptyGuid(MaterialId, nameof(MaterialId));
        DomainInvariants.RequireNonEmpty(MaterialName, nameof(MaterialName));
        DomainInvariants.RequireNonEmpty(Article, nameof(Article));
        DomainInvariants.RequirePositive(Quantity, nameof(Quantity));
        DomainInvariants.RequireNonNegative(UnitPrice, nameof(UnitPrice));
        DomainInvariants.RequireNonNegative(WastePercent, nameof(WastePercent));
    }

    public CostSpecificationLine WithMergedQuantity(decimal additionalQuantity) =>
        new()
        {
            MaterialId = MaterialId,
            ConstructionMaterialId = ConstructionMaterialId,
            Category = Category,
            MaterialName = MaterialName,
            Article = Article,
            Quantity = Quantity + additionalQuantity,
            Unit = Unit,
            UnitPrice = UnitPrice,
            WastePercent = WastePercent
        };
}
