using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Interfaces;

public enum MaterialPriceUnit
{
    LinearMeter,
    SquareMeter,
    Piece,
    Kilogram,
    Set
}

public sealed record MaterialPriceQuote(
    Guid MaterialId,
    string MaterialName,
    string Article,
    MaterialPriceUnit Unit,
    decimal UnitPrice,
    decimal WastePercent,
    string CurrencyCode = "KGS",
    MaterialPriceSource? Source = null);

public interface IMaterialPriceCatalog
{
    MaterialPriceQuote? TryGetByMaterialId(Guid materialId);

    MaterialPriceQuote? TryGetForElement(GeometryElement element, GeometryElementKind effectiveKind);

    MaterialPriceQuote? TryGetByCategory(CostMaterialCategory category, MaterialPriceUnit unit);
}
