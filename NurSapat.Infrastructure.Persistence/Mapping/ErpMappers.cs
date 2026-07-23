using NurSapat.Infrastructure.Persistence.Entities;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Mapping;

internal static class ErpMaterialMapper
{
    public static ErpMaterialDetail ToModel(MaterialEntity entity) => new()
    {
        Id = entity.Id,
        Article = entity.Article,
        Name = entity.Name,
        Note = entity.Note,
        Manufacturer = entity.Manufacturer,
        Section = (ErpMaterialSection)entity.Section,
        MainUnit = (ErpMaterialMainUnit)entity.MainUnit,
        LinearGroup = entity.LinearGroup.HasValue ? (ErpMaterialLinearGroup)entity.LinearGroup.Value : null,
        SquareGroup = entity.SquareGroup.HasValue ? (ErpMaterialSquareGroup)entity.SquareGroup.Value : null,
        Currency = (ErpMaterialCurrency)entity.Currency,
        BasePrice = entity.BasePrice,
        WastePercent = entity.WastePercent,
        PrintOnAssemblyDrawing = entity.PrintOnAssemblyDrawing,
        PrintOnCommercialOffer = entity.PrintOnCommercialOffer,
        PrintOnShippingSheet = entity.PrintOnShippingSheet,
        GroupPrintPositions = entity.GroupPrintPositions,
        IsWarmProfile = entity.IsWarmProfile,
        InnerCoatingAreaPerMeter = entity.InnerCoatingAreaPerMeter,
        OuterCoatingAreaPerMeter = entity.OuterCoatingAreaPerMeter,
        EnableLengthCutOptimization = entity.EnableLengthCutOptimization,
        BarLengthMm = entity.BarLengthMm,
        IlliquidThresholdMm = entity.IlliquidThresholdMm,
        ScrapMm = entity.ScrapMm,
        EnablePlaneCutOptimization = entity.EnablePlaneCutOptimization,
        SheetWidthMm = entity.SheetWidthMm,
        SheetHeightMm = entity.SheetHeightMm,
        GlassTint = entity.GlassTint,
        MinimumCalculatedAreaSqM = entity.MinimumCalculatedAreaSqM,
        UseSecondaryUnit = entity.UseSecondaryUnit,
        SecondaryUnitCoefficient = entity.SecondaryUnitCoefficient,
        PriceByKilogram = entity.PriceByKilogram,
        ColorPrices = entity.ColorPrices.Select(ToModel).ToList(),
        DimensionPrices = entity.DimensionPrices.Select(ToModel).ToList()
    };

    public static MaterialEntity ToEntity(ErpMaterialDetail model) => new()
    {
        Id = model.Id,
        Article = model.Article,
        Name = model.Name,
        Note = model.Note,
        Manufacturer = model.Manufacturer,
        Section = (int)model.Section,
        MainUnit = (int)model.MainUnit,
        LinearGroup = model.LinearGroup.HasValue ? (int)model.LinearGroup.Value : null,
        SquareGroup = model.SquareGroup.HasValue ? (int)model.SquareGroup.Value : null,
        Currency = (int)model.Currency,
        BasePrice = model.BasePrice,
        WastePercent = model.WastePercent,
        PrintOnAssemblyDrawing = model.PrintOnAssemblyDrawing,
        PrintOnCommercialOffer = model.PrintOnCommercialOffer,
        PrintOnShippingSheet = model.PrintOnShippingSheet,
        GroupPrintPositions = model.GroupPrintPositions,
        IsWarmProfile = model.IsWarmProfile,
        InnerCoatingAreaPerMeter = model.InnerCoatingAreaPerMeter,
        OuterCoatingAreaPerMeter = model.OuterCoatingAreaPerMeter,
        EnableLengthCutOptimization = model.EnableLengthCutOptimization,
        BarLengthMm = model.BarLengthMm,
        IlliquidThresholdMm = model.IlliquidThresholdMm,
        ScrapMm = model.ScrapMm,
        EnablePlaneCutOptimization = model.EnablePlaneCutOptimization,
        SheetWidthMm = model.SheetWidthMm,
        SheetHeightMm = model.SheetHeightMm,
        GlassTint = model.GlassTint,
        MinimumCalculatedAreaSqM = model.MinimumCalculatedAreaSqM,
        UseSecondaryUnit = model.UseSecondaryUnit,
        SecondaryUnitCoefficient = model.SecondaryUnitCoefficient,
        PriceByKilogram = model.PriceByKilogram,
        ColorPrices = model.ColorPrices.Select(ToEntity).ToList(),
        DimensionPrices = model.DimensionPrices.Select(ToEntity).ToList()
    };

    private static ErpMaterialColorPrice ToModel(MaterialColorPriceEntity entity) => new()
    {
        Id = entity.Id,
        ColorArticle = entity.ColorArticle,
        ColorName = entity.ColorName,
        ColorHex = entity.ColorHex,
        Price = entity.Price,
        LinkArticle = entity.LinkArticle,
        IsDefault = entity.IsDefault,
        UseCustomCutLength = entity.UseCustomCutLength,
        CustomBarLengthMm = entity.CustomBarLengthMm
    };

    private static MaterialColorPriceEntity ToEntity(ErpMaterialColorPrice model) => new()
    {
        Id = model.Id,
        ColorArticle = model.ColorArticle,
        ColorName = model.ColorName,
        ColorHex = model.ColorHex,
        Price = model.Price,
        LinkArticle = model.LinkArticle,
        IsDefault = model.IsDefault,
        UseCustomCutLength = model.UseCustomCutLength,
        CustomBarLengthMm = model.CustomBarLengthMm
    };

    private static ErpMaterialDimensionPrice ToModel(MaterialDimensionPriceEntity entity) => new()
    {
        Id = entity.Id,
        WidthMm = entity.WidthMm,
        HeightMm = entity.HeightMm,
        Price = entity.Price
    };

    private static MaterialDimensionPriceEntity ToEntity(ErpMaterialDimensionPrice model) => new()
    {
        Id = model.Id,
        WidthMm = model.WidthMm,
        HeightMm = model.HeightMm,
        Price = model.Price
    };

    public static ConstructionElementModel ToModel(ConstructionElementEntity entity) => new()
    {
        Id = entity.Id,
        MaterialId = entity.MaterialId,
        Name = entity.Name,
        ManufacturerId = entity.ManufacturerId,
        Section = (ConstructionSectionTypeModel)entity.Section,
        AssociatedSeriesIds = entity.AssociatedSeries.Select(series => series.SeriesId).ToList(),
        WidthMm = entity.WidthMm,
        HeightMm = entity.HeightMm,
        WallThicknessMm = entity.WallThicknessMm,
        Article = entity.Article,
        WeightKg = entity.WeightKg,
        GlassFormula = entity.GlassFormula,
        GlassThicknessMm = entity.GlassThicknessMm,
        GlassStructure = entity.GlassStructure,
        HardwareRules = entity.HardwareRules.Select(ToModel).ToList(),
        Notes = entity.Notes
    };

    public static ConstructionElementEntity ToEntity(ConstructionElementModel model) => new()
    {
        Id = model.Id,
        MaterialId = model.MaterialId,
        Name = model.Name,
        ManufacturerId = model.ManufacturerId,
        Section = (int)model.Section,
        WidthMm = model.WidthMm,
        HeightMm = model.HeightMm,
        WallThicknessMm = model.WallThicknessMm,
        Article = model.Article,
        WeightKg = model.WeightKg,
        GlassFormula = model.GlassFormula,
        GlassThicknessMm = model.GlassThicknessMm,
        GlassStructure = model.GlassStructure,
        Notes = model.Notes,
        AssociatedSeries = model.AssociatedSeriesIds
            .Select(seriesId => new ConstructionElementSeriesEntity { SeriesId = seriesId })
            .ToList(),
        HardwareRules = model.HardwareRules.Select(ToEntity).ToList()
    };

    private static HardwareAutoPickRuleModel ToModel(HardwareAutoPickRuleEntity entity) => new()
    {
        Id = entity.Id,
        MinWidthFalzMm = entity.MinWidthFalzMm,
        MaxWidthFalzMm = entity.MaxWidthFalzMm,
        MinHeightFalzMm = entity.MinHeightFalzMm,
        MaxHeightFalzMm = entity.MaxHeightFalzMm,
        ScissorsCount = entity.ScissorsCount,
        MiddleLockCount = entity.MiddleLockCount
    };

    private static HardwareAutoPickRuleEntity ToEntity(HardwareAutoPickRuleModel model) => new()
    {
        Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id,
        MinWidthFalzMm = model.MinWidthFalzMm,
        MaxWidthFalzMm = model.MaxWidthFalzMm,
        MinHeightFalzMm = model.MinHeightFalzMm,
        MaxHeightFalzMm = model.MaxHeightFalzMm,
        ScissorsCount = model.ScissorsCount,
        MiddleLockCount = model.MiddleLockCount
    };
}

internal static class WarehouseMapper
{
    public static StockDocumentModel ToModel(StockDocumentEntity entity) => new()
    {
        Id = entity.Id,
        Number = entity.Number,
        Date = entity.Date,
        Type = (StockDocumentTypeModel)entity.Type,
        Note = entity.Note,
        UseCustomExchangeRates = entity.UseCustomExchangeRates,
        Lines = entity.Lines.Select(ToModel).ToList()
    };

    public static StockDocumentEntity ToEntity(StockDocumentModel model) => new()
    {
        Id = model.Id,
        Number = model.Number,
        Date = model.Date,
        Type = (int)model.Type,
        Note = model.Note,
        UseCustomExchangeRates = model.UseCustomExchangeRates,
        Lines = model.Lines.Select(ToEntity).ToList()
    };

    private static StockDocumentLineModel ToModel(StockDocumentLineEntity entity) => new()
    {
        Id = entity.Id,
        MaterialId = entity.MaterialId,
        Article = entity.Article,
        Name = entity.Name,
        Color = entity.Color,
        Unit = entity.Unit,
        Quantity = entity.Quantity,
        Price = entity.Price,
        Currency = entity.Currency
    };

    private static StockDocumentLineEntity ToEntity(StockDocumentLineModel model) => new()
    {
        Id = model.Id,
        MaterialId = model.MaterialId,
        Article = model.Article,
        Name = model.Name,
        Color = model.Color,
        Unit = model.Unit,
        Quantity = model.Quantity,
        Price = model.Price,
        Currency = model.Currency
    };

    public static FinishedGoodModel ToModel(FinishedGoodEntity entity) => new()
    {
        Id = entity.Id,
        OrderId = entity.OrderId,
        OrderNumber = entity.OrderNumber,
        ProductCode = entity.ProductCode,
        CellAddress = entity.CellAddress,
        State = (FinishedGoodStateModel)entity.State,
        DateIncoming = entity.DateIncoming,
        UserName = entity.UserName
    };

    public static FinishedGoodEntity ToEntity(FinishedGoodModel model) => new()
    {
        Id = model.Id,
        OrderId = model.OrderId,
        OrderNumber = model.OrderNumber,
        ProductCode = model.ProductCode,
        CellAddress = model.CellAddress,
        State = (int)model.State,
        DateIncoming = model.DateIncoming,
        UserName = model.UserName
    };
}
