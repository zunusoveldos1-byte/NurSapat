using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Миграция legacy-проектов (v2 fictitious + TransitionFrame) в ConstructionAssembly v3.
/// </summary>
public static class LegacyAssemblyMigrator
{
    public const int TargetCadSchemaVersion = 3;

    public static bool NeedsMigration(
        int schemaVersion,
        FrameNode? frameRoot,
        IReadOnlyList<GeometryElement> elements,
        bool hasAssemblyData) =>
        !hasAssemblyData
        && schemaVersion < TargetCadSchemaVersion
        && frameRoot?.IsFictitiousContainer == true
        && elements.Any(e => e.Kind == GeometryElementKind.TransitionFrame);

    public static ConstructionAssembly Migrate(
        FrameNode globalRoot,
        IReadOnlyList<GeometryElement> elements,
        double coverStripMm = ConstructionAssemblyBuilder.DefaultTransitionCoverStripMm)
    {
        var transitions = ExtractTransitionProfiles(elements, globalRoot);
        return ConstructionAssemblyBuilder.Build(globalRoot, transitions, coverStripMm);
    }

    public static IReadOnlyList<TransitionProfileModel> ExtractTransitionProfiles(
        IReadOnlyList<GeometryElement> elements,
        FrameNode globalRoot)
    {
        var profiles = new List<TransitionProfileModel>();

        foreach (var element in elements.Where(e => e.Kind == GeometryElementKind.TransitionFrame))
        {
            var isVertical = (double)element.WidthMm <= (double)element.HeightMm * 1.25;
            var thickness = ResolveThickness(element);
            var axis = isVertical ? (double)element.X : (double)element.Y;

            profiles.Add(new TransitionProfileModel
            {
                Id = element.Id,
                Orientation = isVertical
                    ? GridLineOrientation.Vertical
                    : GridLineOrientation.Horizontal,
                AxisCoordinate = CadMath.RoundMm(axis),
                ThicknessMm = thickness,
                MaterialId = element.MaterialId,
                HostGridLineId = element.HostGridLineId,
                PartitionNodeId = element.HostFrameNodeId
            });
        }

        return profiles
            .OrderBy(p => p.Orientation == GridLineOrientation.Vertical
                ? p.AxisCoordinate
                : globalRoot.X)
            .ThenBy(p => p.AxisCoordinate)
            .ToList();
    }

    private static double ResolveThickness(GeometryElement element)
    {
        var material = element.MaterialId is Guid id
            ? ConstructionMaterialCatalog.FindById(id)
            : null;
        if (material?.ProfileWidth > 0)
        {
            return material.ProfileWidth;
        }

        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        return CadMath.RoundMm(Math.Min(width, height));
    }
}
