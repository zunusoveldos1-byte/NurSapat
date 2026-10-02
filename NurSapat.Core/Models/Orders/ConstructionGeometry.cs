namespace NurSapat.Core.Models.Orders;

public sealed class ConstructionGeometry
{
    public decimal OuterWidthMm { get; set; }

    public decimal OuterHeightMm { get; set; }

    public string DrawingJson { get; set; } = "{}";

    public List<GeometryElement> Elements { get; } = [];

    public decimal ProductAreaSqM =>
        OuterWidthMm > 0 && OuterHeightMm > 0
            ? decimal.Round(OuterWidthMm * OuterHeightMm / 1_000_000m, 4, MidpointRounding.AwayFromZero)
            : 0m;

    /// <summary>Площадь для калькуляции материалов — без фиктивных контейнеров.</summary>
    public decimal BillableProductAreaSqM
    {
        get
        {
            var billable = FictitiousFrame.EnumerateBillableElements(Elements).ToList();
            if (billable.Count == 0)
            {
                return 0m;
            }

            var glazingArea = billable
                .Where(element => element.Kind == GeometryElementKind.Glazing)
                .Sum(element => element.WidthMm * element.HeightMm / 1_000_000m);
            if (glazingArea > 0)
            {
                return decimal.Round(glazingArea, 4, MidpointRounding.AwayFromZero);
            }

            var frames = billable
                .Where(element => element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                    or GeometryElementKind.TransitionFrame)
                .ToList();
            if (frames.Count > 0)
            {
                var width = frames.Max(element => element.WidthMm);
                var height = frames.Max(element => element.HeightMm);
                return decimal.Round(width * height / 1_000_000m, 4, MidpointRounding.AwayFromZero);
            }

            if (Elements.Any(element => FictitiousFrame.IsFictitiousElement(element)))
            {
                return 0m;
            }

            return ProductAreaSqM;
        }
    }

    public (decimal WidthMm, decimal HeightMm) ResolveBillableOuterBounds()
    {
        var billable = FictitiousFrame.EnumerateBillableElements(Elements).ToList();
        if (billable.Count == 0)
        {
            return (0m, 0m);
        }

        var frames = billable
            .Where(element => element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                or GeometryElementKind.TransitionFrame)
            .ToList();
        if (frames.Count > 0)
        {
            return (frames.Max(element => element.WidthMm), frames.Max(element => element.HeightMm));
        }

        if (Elements.Any(element => FictitiousFrame.IsFictitiousElement(element)))
        {
            return (0m, 0m);
        }

        return (OuterWidthMm, OuterHeightMm);
    }
}
