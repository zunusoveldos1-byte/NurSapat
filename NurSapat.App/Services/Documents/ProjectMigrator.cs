using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Assembly;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services.Documents;

/// <summary>
/// Автоматическая миграция проектов при загрузке (SchemaVersion → 3).
/// </summary>
public static class ProjectMigrator
{
    public static ProjectDocumentData MigrateIfNeeded(ProjectDocumentData project)
    {
        if (project.SchemaVersion >= ProjectFormat.CurrentSchemaVersion
            && project.Cad.Assembly?.SubFrames.Count > 0)
        {
            return project;
        }

        var frameRoot = project.Cad.FrameRoot is null
            ? null
            : FrameNodeMapper.FromData(project.Cad.FrameRoot);

        var elements = project.Order.Constructions
            .SelectMany(c => c.Geometry.Elements)
            .Select(GeometryElementMapper.FromData)
            .ToList();

        if (!LegacyAssemblyMigrator.NeedsMigration(
                project.SchemaVersion,
                frameRoot,
                elements,
                project.Cad.Assembly?.SubFrames.Count > 0))
        {
            if (project.SchemaVersion < ProjectFormat.CurrentSchemaVersion)
            {
                project.SchemaVersion = ProjectFormat.CurrentSchemaVersion;
            }

            return project;
        }

        if (frameRoot is null)
        {
            if (elements.Count > 0)
            {
                throw new InvalidDataException(
                    "Legacy project contains construction elements but no FrameRoot to migrate.");
            }

            project.SchemaVersion = ProjectFormat.CurrentSchemaVersion;
            return project;
        }

        var assembly = LegacyAssemblyMigrator.Migrate(frameRoot, elements);
        project.Cad.Assembly = ConstructionAssemblyMapper.ToData(assembly);
        AssignSubFrameIds(project, assembly);
        project.SchemaVersion = ProjectFormat.CurrentSchemaVersion;
        return project;
    }

    private static void AssignSubFrameIds(ProjectDocumentData project, ConstructionAssembly assembly)
    {
        foreach (var construction in project.Order.Constructions)
        {
            foreach (var element in construction.Geometry.Elements)
            {
                if (Enum.TryParse<GeometryElementKind>(element.Kind, out var kind)
                    && kind == GeometryElementKind.TransitionFrame)
                {
                    element.SubFrameId = null;
                    element.HostFrameNodeId = null;
                    element.HostGridLineId = null;
                    continue;
                }

                if (element.SubFrameId is not null && element.SubFrameId != Guid.Empty)
                {
                    continue;
                }

                var worldX = (double)element.X;
                var worldY = (double)element.Y;
                var owner = assembly.FindOwnerAt(worldX, worldY);
                if (owner is not null)
                {
                    element.SubFrameId = owner.Id;
                }
                else
                {
                    throw new InvalidDataException(
                        $"Legacy element {element.Id} cannot be assigned to a SubFrame.");
                }
            }
        }
    }

    private static class GeometryElementMapper
    {
        public static GeometryElement FromData(GeometryElementData data)
        {
            Enum.TryParse<GeometryElementKind>(data.Kind, out var kind);
            return new GeometryElement
            {
                Id = data.Id,
                Kind = kind,
                X = data.X,
                Y = data.Y,
                WidthMm = data.WidthMm,
                HeightMm = data.HeightMm,
                MaterialId = data.MaterialId,
                HostFrameNodeId = data.HostFrameNodeId,
                HostGridLineId = data.HostGridLineId,
                SubFrameId = data.SubFrameId
            };
        }
    }
}
