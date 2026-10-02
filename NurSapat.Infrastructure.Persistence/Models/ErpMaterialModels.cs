namespace NurSapat.Infrastructure.Persistence.Models;

public sealed class ErpMaterialDetail
{
    public Guid Id { get; set; }

    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public ErpMaterialSection Section { get; set; }

    public ErpMaterialMainUnit MainUnit { get; set; }

    public ErpMaterialLinearGroup? LinearGroup { get; set; }

    public ErpMaterialSquareGroup? SquareGroup { get; set; }

    public ErpMaterialCurrency Currency { get; set; }

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

    public List<ErpMaterialColorPrice> ColorPrices { get; set; } = [];

    public List<ErpMaterialDimensionPrice> DimensionPrices { get; set; } = [];
}

public sealed class ErpMaterialColorPrice
{
    public Guid Id { get; set; }

    public string ColorArticle { get; set; } = string.Empty;

    public string ColorName { get; set; } = string.Empty;

    public string ColorHex { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string LinkArticle { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool UseCustomCutLength { get; set; }

    public decimal? CustomBarLengthMm { get; set; }
}

public sealed class ErpMaterialDimensionPrice
{
    public Guid Id { get; set; }

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public decimal Price { get; set; }
}
