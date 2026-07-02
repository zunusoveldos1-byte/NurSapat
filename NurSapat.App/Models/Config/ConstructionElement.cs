namespace NurSapat.App.Models.Config;

public sealed class ConstructionElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid ManufacturerId { get; set; }

    public ConstructionSectionType Section { get; set; }

    public List<Guid> AssociatedSeriesIds { get; set; } = [];

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public double WallThicknessMm { get; set; }

    public string Article { get; set; } = string.Empty;

    public double WeightKg { get; set; }

    public string GlassFormula { get; set; } = string.Empty;

    public double GlassThicknessMm { get; set; }

    public string GlassStructure { get; set; } = string.Empty;

    public List<HardwareAutoPickRule> HardwareRules { get; set; } = [];

    public string Notes { get; set; } = string.Empty;

    public ConstructionElement Clone() => new()
    {
        Id = Id,
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
