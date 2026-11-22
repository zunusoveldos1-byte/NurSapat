using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

internal static class CostLineBuilder
{
    public static CostSpecificationLine? CreateFromQuote(
        MaterialPriceQuote quote,
        CostMaterialCategory category,
        decimal quantity,
        string unit,
        Guid? constructionMaterialId = null)
    {
        if (quantity <= 0 || quote.MaterialId == Guid.Empty)
        {
            return null;
        }

        return new CostSpecificationLine
        {
            MaterialId = quote.MaterialId,
            ConstructionMaterialId = constructionMaterialId,
            Category = category,
            MaterialName = quote.MaterialName,
            Article = quote.Article,
            Quantity = ApplyWaste(quantity, quote.WastePercent),
            Unit = unit,
            UnitPrice = quote.UnitPrice,
            WastePercent = quote.WastePercent,
            PriceSource = quote.Source
        };
    }

    public static CostSpecificationLine? CreateLinearLine(
        GeometryElement element,
        IMaterialPriceCatalog priceCatalog,
        CostMaterialCategory category,
        decimal lengthM,
        string? fallbackName = null)
    {
        if (lengthM <= 0)
        {
            return null;
        }

        var quote = priceCatalog.TryGetForElement(element, element.Kind);
        if (quote is null)
        {
            return null;
        }

        return CreateFromQuote(quote, category, lengthM, "м/п", element.MaterialId);
    }

    public static CostSpecificationLine? CreateAreaLine(
        GeometryElement element,
        IMaterialPriceCatalog priceCatalog,
        CostMaterialCategory category,
        decimal areaSqM,
        decimal minimumAreaSqM = 0m)
    {
        if (areaSqM <= 0)
        {
            return null;
        }

        var billableArea = minimumAreaSqM > 0
            ? Math.Max(areaSqM, minimumAreaSqM)
            : areaSqM;

        var quote = priceCatalog.TryGetForElement(element, element.Kind);
        if (quote is null)
        {
            return null;
        }

        return CreateFromQuote(quote, category, billableArea, "м²", element.MaterialId);
    }

    public static CostSpecificationLine? CreatePieceLine(
        MaterialPriceQuote quote,
        CostMaterialCategory category,
        decimal quantity,
        string unit,
        Guid? constructionMaterialId = null) =>
        CreateFromQuote(quote, category, quantity, unit, constructionMaterialId);

    public static CostSpecificationLine? CreateCategoryLine(
        IMaterialPriceCatalog priceCatalog,
        CostMaterialCategory category,
        MaterialPriceUnit unit,
        decimal quantity)
    {
        if (quantity <= 0)
        {
            return null;
        }

        var quote = priceCatalog.TryGetByCategory(category, unit);
        if (quote is null)
        {
            return null;
        }

        return CreateFromQuote(quote, category, quantity, MapUnit(quote.Unit));
    }

    public static decimal ApplyWaste(decimal quantity, decimal wastePercent) =>
        wastePercent <= 0
            ? quantity
            : decimal.Round(quantity * (1m + wastePercent / 100m), 4, MidpointRounding.AwayFromZero);

    public static string MapUnit(MaterialPriceUnit unit) => unit switch
    {
        MaterialPriceUnit.LinearMeter => "м/п",
        MaterialPriceUnit.SquareMeter => "м²",
        MaterialPriceUnit.Kilogram => "кг",
        MaterialPriceUnit.Set => "компл.",
        _ => "шт."
    };
}
