using NurSapat.Core.Validation;

namespace NurSapat.App.Models.Config;

/// <summary>Элемент справочника конструкций (профиль, стекло, фурнитура) с привязкой к материалу каталога.</summary>
public sealed class ConstructionElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Идентификатор материала ERP-каталога (<see cref="NurSapat.Core.Models.Materials.CatalogMaterialIds"/>).</summary>
    public Guid MaterialId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid ManufacturerId { get; set; }

    public ConstructionSectionType Section { get; set; }

    public List<Guid> AssociatedSeriesIds { get; set; } = [];

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public double WallThicknessMm { get; set; }

    /// <summary>Денормализованный артикул для отображения (канон — <see cref="MaterialId"/>).</summary>
    public string Article { get; set; } = string.Empty;

    public double WeightKg { get; set; }

    public string GlassFormula { get; set; } = string.Empty;

    public double GlassThicknessMm { get; set; }

    public string GlassStructure { get; set; } = string.Empty;

    public List<HardwareAutoPickRule> HardwareRules { get; set; } = [];

    public string Notes { get; set; } = string.Empty;

    public void Validate()
    {
        DomainInvariants.RequireNonEmptyGuid(MaterialId, nameof(MaterialId));
        DomainInvariants.RequireNonEmpty(Name, nameof(Name));
        DomainInvariants.RequireNonEmpty(Article, nameof(Article));
    }

    public ConstructionElement Clone() => new()
    {
        Id = Id,
        MaterialId = MaterialId,
        Name = Name,
        ManufacturerId = ManufacturerId,
        Section = Section,
        AssociatedSeriesIds = [.. AssociatedSeriesIds],
        WidthMm = WidthMm,
        HeightMm = HeightMm,
        WallThicknessMm = WallThicknessMm,
        Article = Article,
        WeightKg = WeightKg,
        GlassFormula = GlassFormula,
        GlassThicknessMm = GlassThicknessMm,
        GlassStructure = GlassStructure,
        HardwareRules = HardwareRules.Select(rule => rule.Clone()).ToList(),
        Notes = Notes
    };
}
