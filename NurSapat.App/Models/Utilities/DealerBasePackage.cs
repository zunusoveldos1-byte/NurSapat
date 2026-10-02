namespace NurSapat.App.Models.Utilities;

public sealed class DealerBasePackage
{
    public string FormatVersion { get; set; } = "1.0";

    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    public List<DealerMaterialEntry> Materials { get; set; } = [];

    public List<DealerProfileSeriesEntry> ProfileSeries { get; set; } = [];

    public List<DealerManufacturerEntry> Manufacturers { get; set; } = [];
}

public sealed class DealerMaterialEntry
{
    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = "KGS";

    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal WastePercent { get; set; }
}

public sealed class DealerProfileSeriesEntry
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid ManufacturerId { get; set; }

    public double HardwareAxisMm { get; set; }
}

public sealed class DealerManufacturerEntry
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public double FalsCorrectionMm { get; set; }

    public double EndCuttingMm { get; set; }

    public double BladeWidthMm { get; set; }

    public double WeldingShrinkageMm { get; set; }
}
