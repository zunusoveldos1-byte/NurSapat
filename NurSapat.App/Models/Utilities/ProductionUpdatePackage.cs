namespace NurSapat.App.Models.Utilities;

public sealed class ProductionUpdatePackage
{
    public string FormatVersion { get; set; } = "1.0";

    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    public List<DealerMaterialEntry> Materials { get; set; } = [];

    public List<ProductionHardwareConstraintEntry> HardwareConstraints { get; set; } = [];

    public ProductionTechnologySettings TechnologySettings { get; set; } = new();
}

public sealed class ProductionHardwareConstraintEntry
{
    public string HardwareBrand { get; set; } = string.Empty;

    public int MinWidthMm { get; set; }

    public int MaxWidthMm { get; set; }

    public int MinHeightMm { get; set; }

    public int MaxHeightMm { get; set; }

    public decimal MaxWeightKg { get; set; }
}

public sealed class ProductionTechnologySettings
{
    public string ProfileCutLevel { get; set; } = string.Empty;

    public string ProfileWasteMode { get; set; } = string.Empty;

    public string GlassWasteMode { get; set; } = string.Empty;

    public double MinGlassCutLengthMm { get; set; }

    public double MinGlassCutAreaM2 { get; set; }

    public bool AvoidDeadZoneCuts { get; set; }

    public bool ParallelCutTwoBars { get; set; }
}
