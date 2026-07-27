using NurSapat.App.Models.Config;
using NurSapat.App.Models.Materials;
using NurSapat.App.Models.Stock;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.App.Services.Persistence;

public static class AppPersistenceMappers
{
    public static ErpMaterialDetail ToPersistence(MaterialDetail material) => new()
    {
        Id = material.Id,
        Article = material.Article,
        Name = material.Name,
        Note = material.Note,
        Manufacturer = material.Manufacturer,
        SuppliersText = material.SuppliersText,
        Section = (ErpMaterialSection)material.Section,
        MainUnit = (ErpMaterialMainUnit)material.MainUnit,
        LinearGroup = material.LinearGroup.HasValue ? (ErpMaterialLinearGroup)material.LinearGroup.Value : null,
        SquareGroup = material.SquareGroup.HasValue ? (ErpMaterialSquareGroup)material.SquareGroup.Value : null,
        Currency = (ErpMaterialCurrency)material.Currency,
        BasePrice = material.BasePrice,
        WastePercent = material.WastePercent,
        PrintOnAssemblyDrawing = material.PrintOnAssemblyDrawing,
        PrintOnCommercialOffer = material.PrintOnCommercialOffer,
        PrintOnShippingSheet = material.PrintOnShippingSheet,
        GroupPrintPositions = material.GroupPrintPositions,
        IsWarmProfile = material.IsWarmProfile,
        InnerCoatingAreaPerMeter = material.InnerCoatingAreaPerMeter,
        OuterCoatingAreaPerMeter = material.OuterCoatingAreaPerMeter,
        EnableLengthCutOptimization = material.EnableLengthCutOptimization,
        BarLengthMm = material.BarLengthMm,
        IlliquidThresholdMm = material.IlliquidThresholdMm,
        ScrapMm = material.ScrapMm,
        EnablePlaneCutOptimization = material.EnablePlaneCutOptimization,
        SheetWidthMm = material.SheetWidthMm,
        SheetHeightMm = material.SheetHeightMm,
        GlassTint = material.GlassTint,
        MinimumCalculatedAreaSqM = material.MinimumCalculatedAreaSqM,
        UseSecondaryUnit = material.UseSecondaryUnit,
        SecondaryUnitCoefficient = material.SecondaryUnitCoefficient,
        PriceByKilogram = material.PriceByKilogram,
        ColorPrices = material.ColorPrices.Select(color => new ErpMaterialColorPrice
        {
            Id = color.Id,
            ColorArticle = color.ColorArticle,
            ColorName = color.ColorName,
            ColorHex = color.ColorHex,
            Price = color.Price,
            LinkArticle = color.LinkArticle,
            IsDefault = color.IsDefault,
            UseCustomCutLength = color.UseCustomCutLength,
            CustomBarLengthMm = color.CustomBarLengthMm
        }).ToList(),
        DimensionPrices = material.DimensionPrices.Select(price => new ErpMaterialDimensionPrice
        {
            Id = price.Id,
            WidthMm = price.WidthMm,
            HeightMm = price.HeightMm,
            Price = price.Price
        }).ToList()
    };

    public static MaterialDetail FromPersistence(ErpMaterialDetail material) => new()
    {
        Id = material.Id,
        Article = material.Article,
        Name = material.Name,
        Note = material.Note,
        Manufacturer = material.Manufacturer,
        SuppliersText = material.SuppliersText,
        Section = (MaterialSection)material.Section,
        MainUnit = (MaterialMainUnit)material.MainUnit,
        LinearGroup = material.LinearGroup.HasValue ? (MaterialLinearGroup)material.LinearGroup.Value : null,
        SquareGroup = material.SquareGroup.HasValue ? (MaterialSquareGroup)material.SquareGroup.Value : null,
        Currency = (MaterialCurrency)material.Currency,
        BasePrice = material.BasePrice,
        WastePercent = material.WastePercent,
        PrintOnAssemblyDrawing = material.PrintOnAssemblyDrawing,
        PrintOnCommercialOffer = material.PrintOnCommercialOffer,
        PrintOnShippingSheet = material.PrintOnShippingSheet,
        GroupPrintPositions = material.GroupPrintPositions,
        IsWarmProfile = material.IsWarmProfile,
        InnerCoatingAreaPerMeter = material.InnerCoatingAreaPerMeter,
        OuterCoatingAreaPerMeter = material.OuterCoatingAreaPerMeter,
        EnableLengthCutOptimization = material.EnableLengthCutOptimization,
        BarLengthMm = material.BarLengthMm,
        IlliquidThresholdMm = material.IlliquidThresholdMm,
        ScrapMm = material.ScrapMm,
        EnablePlaneCutOptimization = material.EnablePlaneCutOptimization,
        SheetWidthMm = material.SheetWidthMm,
        SheetHeightMm = material.SheetHeightMm,
        GlassTint = material.GlassTint,
        MinimumCalculatedAreaSqM = material.MinimumCalculatedAreaSqM,
        UseSecondaryUnit = material.UseSecondaryUnit,
        SecondaryUnitCoefficient = material.SecondaryUnitCoefficient,
        PriceByKilogram = material.PriceByKilogram,
        ColorPrices = material.ColorPrices.Select(color => new MaterialColorPrice
        {
            Id = color.Id,
            ColorArticle = color.ColorArticle,
            ColorName = color.ColorName,
            ColorHex = color.ColorHex,
            Price = color.Price,
            LinkArticle = color.LinkArticle,
            IsDefault = color.IsDefault,
            UseCustomCutLength = color.UseCustomCutLength,
            CustomBarLengthMm = color.CustomBarLengthMm
        }).ToList(),
        DimensionPrices = material.DimensionPrices.Select(price => new MaterialDimensionPrice
        {
            Id = price.Id,
            WidthMm = price.WidthMm,
            HeightMm = price.HeightMm,
            Price = price.Price
        }).ToList()
    };

    public static StockDocumentModel ToPersistence(StockDocumentHeader document) => new()
    {
        Id = document.Id,
        Number = document.Number,
        Date = document.Date,
        Type = (StockDocumentTypeModel)document.Type,
        Note = document.Note,
        UseCustomExchangeRates = document.UseCustomExchangeRates,
        Lines = document.Lines.Select(line => new StockDocumentLineModel
        {
            Id = line.Id,
            MaterialId = line.MaterialId,
            Article = line.Article,
            Name = line.Name,
            Color = line.Color,
            Unit = line.Unit,
            Quantity = line.Quantity,
            Price = line.Price,
            Currency = line.Currency
        }).ToList()
    };

    public static StockDocumentHeader FromPersistence(StockDocumentModel document)
    {
        var header = new StockDocumentHeader
        {
            Id = document.Id,
            Number = document.Number,
            Date = document.Date,
            Type = (StockDocumentType)document.Type,
            Note = document.Note,
            UseCustomExchangeRates = document.UseCustomExchangeRates
        };

        foreach (var line in document.Lines)
        {
            header.Lines.Add(new StockDocumentLine
            {
                Id = line.Id,
                MaterialId = line.MaterialId,
                Article = line.Article,
                Name = line.Name,
                Color = line.Color,
                Unit = line.Unit,
                Quantity = line.Quantity,
                Price = line.Price,
                Currency = line.Currency
            });
        }

        return header;
    }

    public static FinishedGoodModel ToPersistence(FinishedGoodItem item) => new()
    {
        Id = item.Id,
        OrderId = item.OrderId,
        OrderNumber = item.OrderNumber,
        ProductCode = item.ProductCode,
        CellAddress = item.CellAddress,
        State = (FinishedGoodStateModel)item.State,
        DateIncoming = item.DateIncoming,
        UserName = item.UserName
    };

    public static FinishedGoodItem FromPersistence(FinishedGoodModel item) => new()
    {
        Id = item.Id,
        OrderId = item.OrderId,
        OrderNumber = item.OrderNumber,
        ProductCode = item.ProductCode,
        CellAddress = item.CellAddress,
        State = (FinishedGoodState)item.State,
        DateIncoming = item.DateIncoming,
        UserName = item.UserName
    };

    public static ConstructionElementModel ToPersistence(ConstructionElement element) => new()
    {
        Id = element.Id,
        MaterialId = element.MaterialId,
        Name = element.Name,
        ManufacturerId = element.ManufacturerId,
        Section = (ConstructionSectionTypeModel)element.Section,
        AssociatedSeriesIds = [.. element.AssociatedSeriesIds],
        WidthMm = element.WidthMm,
        HeightMm = element.HeightMm,
        WallThicknessMm = element.WallThicknessMm,
        Article = element.Article,
        WeightKg = element.WeightKg,
        GlassFormula = element.GlassFormula,
        GlassThicknessMm = element.GlassThicknessMm,
        GlassStructure = element.GlassStructure,
        HardwareRules = element.HardwareRules.Select(rule => new HardwareAutoPickRuleModel
        {
            Id = Guid.NewGuid(),
            MinWidthFalzMm = rule.MinWidthFalzMm,
            MaxWidthFalzMm = rule.MaxWidthFalzMm,
            MinHeightFalzMm = rule.MinHeightFalzMm,
            MaxHeightFalzMm = rule.MaxHeightFalzMm,
            ScissorsCount = rule.ScissorsCount,
            MiddleLockCount = rule.MiddleLockCount
        }).ToList(),
        Notes = element.Notes
    };

    public static ConstructionElement FromPersistence(ConstructionElementModel model) => new()
    {
        Id = model.Id,
        MaterialId = model.MaterialId,
        Name = model.Name,
        ManufacturerId = model.ManufacturerId,
        Section = (ConstructionSectionType)model.Section,
        AssociatedSeriesIds = [.. model.AssociatedSeriesIds],
        WidthMm = model.WidthMm,
        HeightMm = model.HeightMm,
        WallThicknessMm = model.WallThicknessMm,
        Article = model.Article,
        WeightKg = model.WeightKg,
        GlassFormula = model.GlassFormula,
        GlassThicknessMm = model.GlassThicknessMm,
        GlassStructure = model.GlassStructure,
        HardwareRules = model.HardwareRules.Select(rule => new HardwareAutoPickRule
        {
            MinWidthFalzMm = rule.MinWidthFalzMm,
            MaxWidthFalzMm = rule.MaxWidthFalzMm,
            MinHeightFalzMm = rule.MinHeightFalzMm,
            MaxHeightFalzMm = rule.MaxHeightFalzMm,
            ScissorsCount = rule.ScissorsCount,
            MiddleLockCount = rule.MiddleLockCount
        }).ToList(),
        Notes = model.Notes
    };
}
