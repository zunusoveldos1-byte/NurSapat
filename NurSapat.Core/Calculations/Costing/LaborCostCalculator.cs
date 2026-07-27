using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;

namespace NurSapat.Core.Calculations.Costing;

public sealed class LaborCostCalculator
{
    public IReadOnlyList<CostSpecificationLine> Calculate(IMaterialPriceCatalog priceCatalog)
    {
        var quote = priceCatalog.TryGetByCategory(CostMaterialCategory.Labor, MaterialPriceUnit.Set);
        if (quote is null)
        {
            return [];
        }

        var line = CostLineBuilder.CreatePieceLine(quote, CostMaterialCategory.Labor, 1m, CostLineBuilder.MapUnit(quote.Unit));

        return line is null ? [] : [line];
    }
}
