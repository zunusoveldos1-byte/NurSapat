namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class ConstructionElementEntity
{
    public Guid Id { get; set; }

    public Guid MaterialId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid ManufacturerId { get; set; }

    public int Section { get; set; }

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public double WallThicknessMm { get; set; }

    public string Article { get; set; } = string.Empty;

    public double WeightKg { get; set; }

    public string GlassFormula { get; set; } = string.Empty;

    public double GlassThicknessMm { get; set; }

    public string GlassStructure { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public MaterialEntity Material { get; set; } = null!;

    public ICollection<ConstructionElementSeriesEntity> AssociatedSeries { get; set; } = [];

    public ICollection<HardwareAutoPickRuleEntity> HardwareRules { get; set; } = [];
}

public sealed class ConstructionElementSeriesEntity
{
    public Guid ConstructionElementId { get; set; }

    public Guid SeriesId { get; set; }

    public ConstructionElementEntity ConstructionElement { get; set; } = null!;
}

public sealed class HardwareAutoPickRuleEntity
{
    public Guid Id { get; set; }

    public Guid ConstructionElementId { get; set; }

    public double MinWidthFalzMm { get; set; }

    public double MaxWidthFalzMm { get; set; }

    public double MinHeightFalzMm { get; set; }

    public double MaxHeightFalzMm { get; set; }

    public int ScissorsCount { get; set; }

    public int MiddleLockCount { get; set; }

    public ConstructionElementEntity ConstructionElement { get; set; } = null!;
}
