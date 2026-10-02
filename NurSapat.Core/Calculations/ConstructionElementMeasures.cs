using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations;

/// <summary>Геометрические величины элементов конструкции для расчёта стоимости.</summary>
public static class ConstructionElementMeasures
{
    public static decimal GetLinearLengthM(
        GeometryElement element,
        ConstructionMaterial? material = null)
    {
        var lengthMm = GetLinearLengthMm(element, material);
        return lengthMm <= 0
            ? 0m
            : decimal.Round((decimal)lengthMm / 1000m, 4, MidpointRounding.AwayFromZero);
    }

    public static double GetLinearLengthMm(GeometryElement element, ConstructionMaterial? material = null)
    {
        material ??= element.MaterialId is Guid id
            ? ConstructionMaterialCatalog.FindById(id)
            : null;

        var endCutType = EndCutTypeResolver.Resolve(element, material);
        return element.Kind switch
        {
            GeometryElementKind.RoundDummyFrame when material is not null
                => ResolveRoundFrameLengthMm(element, material),
            GeometryElementKind.TriangleDummyFrame
                => ResolveTriangleFramePerimeterMm(element),
            GeometryElementKind.Mullion or GeometryElementKind.DummyMullion or GeometryElementKind.EdgeMullion
                => ResolveMullionLengthMm(element),
            GeometryElementKind.Frame or GeometryElementKind.Sash or GeometryElementKind.ArchFrame
                or GeometryElementKind.TransitionFrame or GeometryElementKind.Bead
                or GeometryElementKind.SlidingSash
                => element.Shape == ConstructionShape.Triangle
                    ? ResolveTriangleFramePerimeterMm(element)
                    : ResolveRectangularPerimeterMm(element),
            GeometryElementKind.DecorativeSpoke
                => DecorativeSpokeMetricsCalculator.CalculateTotalLengthMm(element),
            GeometryElementKind.Sill
                => (double)element.WidthMm,
            GeometryElementKind.Handle or GeometryElementKind.Hardware
                or GeometryElementKind.Accessory or GeometryElementKind.Installation
                => 1,
            _ => 0
        };
    }

    public static decimal GetAreaSqM(GeometryElement element)
    {
        if (element.Kind is GeometryElementKind.Glazing or GeometryElementKind.Corrugation
            or GeometryElementKind.MosquitoNet)
        {
            return decimal.Round(element.WidthMm * element.HeightMm / 1_000_000m, 4, MidpointRounding.AwayFromZero);
        }

        return 0m;
    }

    public static bool IsLinearProfileKind(GeometryElementKind kind) =>
        kind is GeometryElementKind.Frame or GeometryElementKind.Sash or GeometryElementKind.Mullion
            or GeometryElementKind.EdgeMullion or GeometryElementKind.ArchFrame
            or GeometryElementKind.TransitionFrame or GeometryElementKind.Bead
            or GeometryElementKind.SlidingSash or GeometryElementKind.RoundDummyFrame
            or GeometryElementKind.TriangleDummyFrame or GeometryElementKind.DecorativeSpoke
            or GeometryElementKind.Sill or GeometryElementKind.DummyMullion;

    private static double ResolveTriangleFramePerimeterMm(GeometryElement element)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var halfBase = width / 2.0;
        var side = Math.Sqrt(halfBase * halfBase + height * height);
        return width + side * 2;
    }

    private static double ResolveRoundFrameLengthMm(GeometryElement element, ConstructionMaterial material)
    {
        var externalRadius = (double)element.WidthMm / 2.0;
        var rMid = externalRadius - material.ProfileWidth / 2.0;
        return rMid > 0 ? 2.0 * Math.PI * rMid : 0;
    }

    private static double ResolveMullionLengthMm(GeometryElement element)
    {
        if (element.EndX.HasValue && element.EndY.HasValue)
        {
            var dx = (double)element.EndX.Value - (double)element.X;
            var dy = (double)element.EndY.Value - (double)element.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        return Math.Max((double)element.WidthMm, (double)element.HeightMm);
    }

    private static double ResolveRectangularPerimeterMm(GeometryElement element) =>
        2.0 * ((double)element.WidthMm + (double)element.HeightMm);
}
