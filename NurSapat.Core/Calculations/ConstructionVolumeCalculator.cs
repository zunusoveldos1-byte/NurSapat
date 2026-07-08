using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations;

/// <summary>
/// Расчёт физического объёма конструкций по материалам и сериям профилей.
/// </summary>
public static class ConstructionVolumeCalculator
{
    [ThreadStatic]
    private static int _calculateDepth;

    private const int MaxCalculateDepth = 4;

    public static ConstructionVolumeSummary Calculate(IEnumerable<GeometryElement> elements) =>
        Calculate(elements, ProfileSeriesCatalog.All, ConstructionMaterialCatalog.All);

    public static ConstructionVolumeSummary Calculate(
        IEnumerable<GeometryElement> elements,
        IReadOnlyList<ProfileSeries> seriesCatalog,
        IReadOnlyList<ConstructionMaterial> materialCatalog)
    {
        if (_calculateDepth >= MaxCalculateDepth)
        {
            return new ConstructionVolumeSummary(0, []);
        }

        _calculateDepth++;
        try
        {
            var seriesById = seriesCatalog.ToDictionary(series => series.Id);
            var materialsById = materialCatalog.ToDictionary(material => material.Id);
            var lines = new List<ConstructionVolumeLine>();
            var total = 0.0;

            foreach (var element in elements)
            {
                var volume = CalculateElementVolumeMm3(element, seriesById, materialsById, out var material, out var series);
                lines.Add(new ConstructionVolumeLine(
                    element.Id,
                    element.Kind,
                    element.Label,
                    volume,
                    element.IsBillable,
                    material?.Name,
                    series?.Name,
                    EndCutTypeResolver.Resolve(element, material)));

                total += volume;
            }

            return new ConstructionVolumeSummary(total, lines);
        }
        finally
        {
            _calculateDepth--;
        }
    }

    public static double CalculateElementVolumeMm3(GeometryElement element) =>
        CalculateElementVolumeMm3(
            element,
            ProfileSeriesCatalog.All.ToDictionary(series => series.Id),
            ConstructionMaterialCatalog.All.ToDictionary(material => material.Id),
            out _,
            out _);

    public static double CalculateElementVolumeMm3(
        GeometryElement element,
        IReadOnlyDictionary<Guid, ProfileSeries> seriesById,
        IReadOnlyDictionary<Guid, ConstructionMaterial> materialsById,
        out ConstructionMaterial? material,
        out ProfileSeries? series)
    {
        material = null;
        series = null;

        if (!element.IsBillable || !element.MaterialId.HasValue)
        {
            return 0;
        }

        if (!materialsById.TryGetValue(element.MaterialId.Value, out material))
        {
            return 0;
        }

        if (element.ProfileSeriesId is Guid seriesId && seriesById.TryGetValue(seriesId, out var resolvedSeries))
        {
            series = resolvedSeries;
        }

        var crossSection = ResolveCrossSectionArea(material, series);
        if (crossSection <= 0)
        {
            return 0;
        }

        var endCutType = EndCutTypeResolver.Resolve(element, material);
        var lengthMm = ResolveBlankLengthMm(element, material, endCutType);

        if (element.Kind is GeometryElementKind.Corrugation or GeometryElementKind.MosquitoNet)
        {
            var areaMm2 = (double)element.WidthMm * (double)element.HeightMm;
            var panelThickness = material.ProfileWidth > 0 ? material.ProfileWidth : 10;
            return areaMm2 * panelThickness;
        }

        if (element.Kind == GeometryElementKind.Sill)
        {
            var sillWidth = element.EndX.HasValue && element.EndX != element.X
                ? Math.Abs((double)element.EndX.Value - (double)element.X) * 2
                : (double)element.WidthMm;
            return sillWidth * material.ProfileWidth;
        }

        return lengthMm * crossSection;
    }

    private static double ResolveCrossSectionArea(ConstructionMaterial material, ProfileSeries? series)
    {
        if (material.CrossSectionArea > 0)
        {
            return material.CrossSectionArea;
        }

        if (material.ApplicableKind == GeometryElementKind.DecorativeSpoke)
        {
            return material.ProfileWidth * 10.0;
        }

        var depth = series?.SystemDepth ?? 70.0;
        return depth * material.ProfileWidth;
    }

    private static double ResolveBlankLengthMm(
        GeometryElement element,
        ConstructionMaterial material,
        EndCutType endCutType)
    {
        return element.Kind switch
        {
            GeometryElementKind.RoundDummyFrame => ResolveRoundFrameLengthMm(element, material),
            GeometryElementKind.TriangleDummyFrame => ResolveTriangleFramePerimeterMm(element),
            GeometryElementKind.Corrugation or GeometryElementKind.MosquitoNet
                => Math.Sqrt((double)element.WidthMm * (double)element.HeightMm),
            GeometryElementKind.Sill => (double)element.WidthMm,
            GeometryElementKind.Mullion or GeometryElementKind.DummyMullion or GeometryElementKind.EdgeMullion
                => ResolveMullionLengthMm(element),
            GeometryElementKind.Frame or GeometryElementKind.Sash or GeometryElementKind.ArchFrame
                or GeometryElementKind.TransitionFrame or GeometryElementKind.Bead
                => ResolveRectangularPerimeterMm(element, endCutType),
            GeometryElementKind.DecorativeSpoke
                => DecorativeSpokeMetricsCalculator.CalculateTotalLengthMm(element),
            _ => 0
        };
    }

    private static double ResolveTriangleFramePerimeterMm(GeometryElement element)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var halfBase = width / 2.0;
        var side = Math.Sqrt(halfBase * halfBase + height * height);
        return width + side * 2;
    }

    /// <summary>L = 2π × R_mid, R_mid = R_ext − W_profile/2.</summary>
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

    private static double ResolveRectangularPerimeterMm(GeometryElement element, EndCutType endCutType)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var perimeter = 2.0 * (width + height);

        // Для Mitre45 длина заготовки = периметр средней линии (без дополнительных выемок на демо-этапе).
        return endCutType == EndCutType.ButtJoint0
            ? perimeter
            : perimeter;
    }
}

public sealed record ConstructionVolumeLine(
    Guid ElementId,
    GeometryElementKind Kind,
    string Label,
    double VolumeMm3,
    bool IsBillable,
    string? MaterialName,
    string? SeriesName,
    EndCutType EndCutType);

public sealed record ConstructionVolumeSummary(
    double TotalVolumeMm3,
    IReadOnlyList<ConstructionVolumeLine> Lines)
{
    public double TotalVolumeLiters => TotalVolumeMm3 / 1_000_000.0;
}
