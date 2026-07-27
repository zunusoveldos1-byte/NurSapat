namespace NurSapat.Infrastructure.Sync.Models;

/// <summary>
/// Local material row mirroring the cloud materials table plus sync metadata.
/// Field set matches NurSapat MaterialEntity (ERP catalog).
/// </summary>
public sealed class LocalMaterial
{
    public Guid Id { get; set; }

    public string Article { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;

    public int Section { get; set; }
    public int MainUnit { get; set; }
    public int? LinearGroup { get; set; }
    public int? SquareGroup { get; set; }
    public int Currency { get; set; }

    public decimal BasePrice { get; set; }
    public decimal WastePercent { get; set; }

    public bool PrintOnAssemblyDrawing { get; set; }
    public bool PrintOnCommercialOffer { get; set; }
    public bool PrintOnShippingSheet { get; set; }
    public bool GroupPrintPositions { get; set; }
    public bool IsWarmProfile { get; set; }

    public decimal InnerCoatingAreaPerMeter { get; set; }
    public decimal OuterCoatingAreaPerMeter { get; set; }

    public bool EnableLengthCutOptimization { get; set; }
    public decimal BarLengthMm { get; set; }
    public decimal IlliquidThresholdMm { get; set; }
    public decimal ScrapMm { get; set; }

    public bool EnablePlaneCutOptimization { get; set; }
    public decimal SheetWidthMm { get; set; }
    public decimal SheetHeightMm { get; set; }

    public decimal GlassTint { get; set; }
    public decimal MinimumCalculatedAreaSqM { get; set; }

    public bool UseSecondaryUnit { get; set; }
    public decimal SecondaryUnitCoefficient { get; set; }
    public bool PriceByKilogram { get; set; }

    /// <summary>JSON snapshot of color/dimension price child rows (local + cloud).</summary>
    public string PricesJson { get; set; } = "[]";

    public Guid? UserId { get; set; }
    public bool IsDeleted { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LastSyncError { get; set; }
}
