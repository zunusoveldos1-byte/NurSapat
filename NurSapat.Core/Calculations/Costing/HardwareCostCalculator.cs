using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Settings;

namespace NurSapat.Core.Calculations.Costing;

public sealed class HardwareCostCalculator
{
    private readonly CalculationEngine _engine;

    public HardwareCostCalculator(ProductionOptions? options = null) =>
        _engine = new CalculationEngine(options);

    public IReadOnlyList<CostSpecificationLine> Calculate(
        IEnumerable<GeometryElement> elements,
        IMaterialPriceCatalog priceCatalog)
    {
        var lines = new List<CostSpecificationLine>();
        var list = elements.ToList();

        foreach (var element in list)
        {
            if (element.Kind is GeometryElementKind.Hardware or GeometryElementKind.Handle)
            {
                var quote = priceCatalog.TryGetForElement(element, element.Kind);
                if (quote is not null)
                {
                    var line = CostLineBuilder.CreatePieceLine(quote, CostMaterialCategory.Hardware, 1m, "шт.", element.MaterialId);
                    if (line is not null)
                    {
                        lines.Add(line);
                    }
                }
            }
        }

        foreach (var sash in list.Where(IsHardwareSash))
        {
            var kit = _engine.CalculateHardwareKit(sash.WidthMm, sash.HeightMm);
            AddHardwareComponent(lines, priceCatalog, "Петли", kit.Hinges, "шт.");
            AddHardwareComponent(lines, priceCatalog, "Ручки", kit.Handles, "шт.");
            AddHardwareComponent(lines, priceCatalog, "Ответные планки", kit.StrikerPlates, "шт.");
            AddHardwareComponent(lines, priceCatalog, "Саморезы короткие", kit.ShortScrews, "шт.");
            AddHardwareComponent(lines, priceCatalog, "Саморезы длинные", kit.LongScrews, "шт.");
        }

        return lines;
    }

    private static bool IsHardwareSash(GeometryElement element) =>
        element.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash
        && (element.HasHardware || element.OpeningType is not null and not SashOpeningType.Fixed);

    private static void AddHardwareComponent(
        List<CostSpecificationLine> lines,
        IMaterialPriceCatalog priceCatalog,
        string fallbackName,
        int quantity,
        string unit)
    {
        if (quantity <= 0)
        {
            return;
        }

        var quote = priceCatalog.TryGetByCategory(CostMaterialCategory.Hardware, MaterialPriceUnit.Piece);
        if (quote is null)
        {
            return;
        }

        var line = CostLineBuilder.CreatePieceLine(quote, CostMaterialCategory.Hardware, quantity, unit);
        if (line is not null)
        {
            lines.Add(line);
        }
    }
}
