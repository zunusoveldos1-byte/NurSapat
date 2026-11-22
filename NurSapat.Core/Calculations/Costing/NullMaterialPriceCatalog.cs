using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

public sealed class NullMaterialPriceCatalog : IMaterialPriceCatalog
{
    public static NullMaterialPriceCatalog Instance { get; } = new();

    public MaterialPriceQuote? TryGetByMaterialId(Guid materialId) => null;

    public MaterialPriceQuote? TryGetForElement(GeometryElement element, GeometryElementKind effectiveKind) => null;

    public MaterialPriceQuote? TryGetByCategory(CostMaterialCategory category, MaterialPriceUnit unit) => null;
}
