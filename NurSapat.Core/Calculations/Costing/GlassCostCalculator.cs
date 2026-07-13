using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

public sealed class GlassCostCalculator
{
    public IReadOnlyList<CostSpecificationLine> Calculate(
        IEnumerable<GeometryElement> elements,
        IMaterialPriceCatalog priceCatalog)
    {
        var lines = new List<CostSpecificationLine>();

        foreach (var element in elements)
        {
            if (element.Kind is not (GeometryElementKind.Glazing or GeometryElementKind.Corrugation))
            {
                continue;
            }

            var areaSqM = ConstructionElementMeasures.GetAreaSqM(element);
            var category = element.Kind == GeometryElementKind.Glazing
                ? CostMaterialCategory.Glazing
                : CostMaterialCategory.Components;

            var line = CostLineBuilder.CreateAreaLine(element, priceCatalog, category, areaSqM);
            if (line is not null)
            {
                lines.Add(line);
            }
        }

        return lines;
    }
}
