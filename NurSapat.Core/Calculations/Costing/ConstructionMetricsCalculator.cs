using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations.Costing;

public static class ConstructionMetricsCalculator
{
    public static ConstructionMetrics Calculate(OrderConstruction construction)
    {
        var geometry = construction.Geometry;
        var billable = geometry.Elements
            .Where(element => element.IsBillable)
            .ToList();

        var glazingArea = billable
            .Where(element => element.Kind == GeometryElementKind.Glazing)
            .Sum(ConstructionElementMeasures.GetAreaSqM);

        var productArea = geometry.BillableProductAreaSqM;
        if (productArea <= 0 && billable.Count > 0)
        {
            var frames = billable
                .Where(element => element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                    or GeometryElementKind.TransitionFrame)
                .ToList();
            if (frames.Count > 0)
            {
                var width = frames.Max(element => element.WidthMm);
                var height = frames.Max(element => element.HeightMm);
                productArea = decimal.Round(width * height / 1_000_000m, 4, MidpointRounding.AwayFromZero);
            }
        }

        var volumeSummary = ConstructionVolumeCalculator.Calculate(
            billable,
            ProfileSeriesCatalog.All,
            ConstructionMaterialCatalog.All);

        var weightKg = volumeSummary.TotalVolumeLiters > 0
            ? decimal.Round((decimal)volumeSummary.TotalVolumeLiters * 1.4m, 1, MidpointRounding.AwayFromZero)
            : 0m;

        var sashCount = billable.Count(element =>
            element.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash);
        var profileLengthM = billable
            .Where(element => ConstructionElementMeasures.IsLinearProfileKind(element.Kind))
            .Sum(element => ConstructionElementMeasures.GetLinearLengthM(element));

        var manufacturingMinutes = 20m
            + sashCount * 12m
            + profileLengthM * 2m
            + productArea * 30m;

        return new ConstructionMetrics
        {
            ProductAreaSqM = productArea,
            GlazingAreaSqM = decimal.Round(glazingArea, 3, MidpointRounding.AwayFromZero),
            WeightKg = weightKg,
            ManufacturingTime = TimeSpan.FromMinutes((double)Math.Max(0m, manufacturingMinutes))
        };
    }
}
