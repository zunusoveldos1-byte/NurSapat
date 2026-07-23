using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

public sealed class ProfileCostCalculator
{
    public IReadOnlyList<CostSpecificationLine> Calculate(
        IEnumerable<GeometryElement> elements,
        IMaterialPriceCatalog priceCatalog)
    {
        var lines = new List<CostSpecificationLine>();

        foreach (var element in elements)
        {
            if (!ConstructionElementMeasures.IsLinearProfileKind(element.Kind))
            {
                continue;
            }

            if (element.Kind is GeometryElementKind.Bead)
            {
                continue;
            }

            var lengthM = ConstructionElementMeasures.GetLinearLengthM(element);
            var line = CostLineBuilder.CreateLinearLine(
                element,
                priceCatalog,
                CostMaterialCategory.Profile,
                lengthM);
            if (line is not null)
            {
                lines.Add(line);
            }
        }

        return lines;
    }
}
