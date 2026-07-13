namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class MaterialEntity
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

    public ICollection<MaterialColorPriceEntity> ColorPrices { get; set; } = [];

    public ICollection<MaterialDimensionPriceEntity> DimensionPrices { get; set; } = [];
}

public sealed class MaterialColorPriceEntity
{
    public Guid Id { get; set; }

    public Guid MaterialId { get; set; }

    public string ColorArticle { get; set; } = string.Empty;

    public string ColorName { get; set; } = string.Empty;

    public string ColorHex { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string LinkArticle { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool UseCustomCutLength { get; set; }

    public decimal? CustomBarLengthMm { get; set; }

    public MaterialEntity Material { get; set; } = null!;
}

public sealed class MaterialDimensionPriceEntity
{
    public Guid Id { get; set; }

    public Guid MaterialId { get; set; }

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public decimal Price { get; set; }

    public MaterialEntity Material { get; set; } = null!;
}
