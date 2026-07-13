using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

public sealed class AccessoryCostCalculator
{
    public IReadOnlyList<CostSpecificationLine> Calculate(
        IEnumerable<GeometryElement> elements,
        IMaterialPriceCatalog priceCatalog)
    {
        var lines = new List<CostSpecificationLine>();
        var list = elements.ToList();

        foreach (var element in list)
        {
            switch (element.Kind)
            {
                case GeometryElementKind.Bead:
                {
                    var lengthM = ConstructionElementMeasures.GetLinearLengthM(element);
                    var line = CostLineBuilder.CreateLinearLine(
                        element,
                        priceCatalog,
                        CostMaterialCategory.Components,
                        lengthM);
                    if (line is not null)
                    {
                        lines.Add(line);
                    }

                    break;
                }
                case GeometryElementKind.MosquitoNet:
                {
                    var areaSqM = ConstructionElementMeasures.GetAreaSqM(element);
                    var line = CostLineBuilder.CreateAreaLine(
                        element,
                        priceCatalog,
                        CostMaterialCategory.Components,
                        areaSqM);
                    if (line is not null)
                    {
                        lines.Add(line);
                    }

                    break;
                }
                case GeometryElementKind.Sill:
                {
                    var lengthM = ConstructionElementMeasures.GetLinearLengthM(element);
                    var line = CostLineBuilder.CreateLinearLine(
                        element,
                        priceCatalog,
                        CostMaterialCategory.Installation,
                        lengthM);
                    if (line is not null)
                    {
                        lines.Add(line);
                    }

                    break;
                }
                case GeometryElementKind.DecorativeSpoke:
                {
                    var lengthM = ConstructionElementMeasures.GetLinearLengthM(element);
                    var line = CostLineBuilder.CreateLinearLine(
                        element,
                        priceCatalog,
                        CostMaterialCategory.Components,
                        lengthM);
                    if (line is not null)
                    {
                        lines.Add(line);
                    }

                    break;
                }
                case GeometryElementKind.Accessory or GeometryElementKind.Installation:
                {
                    var quote = priceCatalog.TryGetForElement(element, element.Kind);
                    if (quote is not null)
                    {
                        var line = CostLineBuilder.CreatePieceLine(
                            quote,
                            CostMaterialCategory.Components,
                            1m,
                            CostLineBuilder.MapUnit(quote.Unit),
                            element.MaterialId);
                        if (line is not null)
                        {
                            lines.Add(line);
                        }
                    }

                    break;
                }
            }
        }

        var framePerimeterM = list
            .Where(element => element.Kind is GeometryElementKind.Frame or GeometryElementKind.Sash
                or GeometryElementKind.ArchFrame or GeometryElementKind.TransitionFrame
                or GeometryElementKind.SlidingSash)
            .Sum(element => ConstructionElementMeasures.GetLinearLengthM(element));

        if (framePerimeterM > 0)
        {
            var reinforcement = CostLineBuilder.CreateCategoryLine(
                priceCatalog,
                CostMaterialCategory.Components,
                MaterialPriceUnit.LinearMeter,
                framePerimeterM);
            if (reinforcement is not null)
            {
                lines.Add(reinforcement);
            }

            var sealant = CostLineBuilder.CreateCategoryLine(
                priceCatalog,
                CostMaterialCategory.Sealant,
                MaterialPriceUnit.LinearMeter,
                framePerimeterM);
            if (sealant is not null)
            {
                lines.Add(sealant);
            }
        }

        var connectorCount = list.Count(element =>
            element.Kind is GeometryElementKind.Mullion or GeometryElementKind.EdgeMullion);
        if (connectorCount > 0)
        {
            var connectors = CostLineBuilder.CreateCategoryLine(
                priceCatalog,
                CostMaterialCategory.Components,
                MaterialPriceUnit.Piece,
                connectorCount);
            if (connectors is not null)
            {
                lines.Add(connectors);
            }
        }

        return lines;
    }
}
