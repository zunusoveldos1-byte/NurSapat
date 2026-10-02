using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Settings;

namespace NurSapat.Core.Calculations.Costing;

public sealed class ConstructionCostCalculator
{
    private readonly IMaterialPriceCatalog _priceCatalog;
    private readonly ProfileCostCalculator _profileCalculator = new();
    private readonly GlassCostCalculator _glassCalculator = new();
    private readonly HardwareCostCalculator _hardwareCalculator;
    private readonly AccessoryCostCalculator _accessoryCalculator = new();
    private readonly LaborCostCalculator _laborCalculator = new();

    public ConstructionCostCalculator(IMaterialPriceCatalog priceCatalog, ProductionOptions? options = null)
    {
        _priceCatalog = priceCatalog;
        _hardwareCalculator = new HardwareCostCalculator(options);
    }

    public ConstructionCostResult Calculate(OrderConstruction construction)
    {
        var billableElements = construction.Geometry.Elements
            .Where(element => element.IsBillable)
            .ToList();

        var lines = new List<CostSpecificationLine>();
        lines.AddRange(_profileCalculator.Calculate(billableElements, _priceCatalog));
        lines.AddRange(_glassCalculator.Calculate(billableElements, _priceCatalog));
        lines.AddRange(_hardwareCalculator.Calculate(billableElements, _priceCatalog));
        lines.AddRange(_accessoryCalculator.Calculate(billableElements, _priceCatalog));
        lines.AddRange(_laborCalculator.Calculate(_priceCatalog));

        var mergedLines = MergeLines(lines);
        var metrics = ConstructionMetricsCalculator.Calculate(construction);

        return new ConstructionCostResult
        {
            Lines = mergedLines,
            Metrics = metrics,
            CategoryTotals = SummarizeCategories(mergedLines)
        };
    }

    private static IReadOnlyList<CostSpecificationLine> MergeLines(IEnumerable<CostSpecificationLine> lines)
    {
        var merged = new Dictionary<string, CostSpecificationLine>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var key = $"{line.MaterialId:N}|{line.Unit}|{line.UnitPrice:0.####}";
            if (merged.TryGetValue(key, out var existing))
            {
                merged[key] = existing.WithMergedQuantity(line.Quantity);
            }
            else
            {
                merged[key] = line;
            }
        }

        return merged.Values
            .OrderBy(line => line.Category)
            .ThenBy(line => line.MaterialName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ConstructionCategoryTotals SummarizeCategories(IEnumerable<CostSpecificationLine> lines)
    {
        var totals = new ConstructionCategoryTotals();
        foreach (var line in lines)
        {
            switch (line.Category)
            {
                case CostMaterialCategory.Profile:
                    totals.ProfileCost += line.LineTotal;
                    break;
                case CostMaterialCategory.Components:
                    totals.ComponentsCost += line.LineTotal;
                    break;
                case CostMaterialCategory.Sealant:
                    totals.SealantCost += line.LineTotal;
                    break;
                case CostMaterialCategory.Glazing:
                    totals.GlazingCost += line.LineTotal;
                    break;
                case CostMaterialCategory.Hardware:
                    totals.HardwareCost += line.LineTotal;
                    break;
                case CostMaterialCategory.Installation:
                    totals.InstallationMaterialsCost += line.LineTotal;
                    break;
                case CostMaterialCategory.Labor:
                    totals.LaborCost += line.LineTotal;
                    break;
            }
        }

        return totals;
    }
}

public sealed class ConstructionCostResult
{
    public IReadOnlyList<CostSpecificationLine> Lines { get; init; } = [];

    public ConstructionMetrics Metrics { get; init; } = new();

    public ConstructionCategoryTotals CategoryTotals { get; init; } = new();
}

public sealed class ConstructionCategoryTotals
{
    public decimal ProfileCost { get; set; }

    public decimal ComponentsCost { get; set; }

    public decimal SealantCost { get; set; }

    public decimal GlazingCost { get; set; }

    public decimal HardwareCost { get; set; }

    public decimal InstallationMaterialsCost { get; set; }

    public decimal LaborCost { get; set; }
}
