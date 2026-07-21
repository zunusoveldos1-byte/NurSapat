using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Assembly;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

/// <summary>Единственный адаптер старых шаблонов без ConstructionAssembly.</summary>
internal static class LegacyTemplateMigration
{
    public static ConstructionMemento ToMemento(ConstructionTemplatePayload payload)
    {
        var assemblyData = payload.Assembly;
        if (assemblyData is null && FrameNode.RestoreTree(payload.FrameRoot) is { } legacyRoot)
        {
            var assembly = LegacyAssemblyMigrator.Migrate(legacyRoot, payload.Elements);
            assemblyData = ConstructionAssemblyMapper.ToData(assembly);
            foreach (var element in payload.Elements)
            {
                if (element.Kind == GeometryElementKind.TransitionFrame)
                {
                    continue;
                }

                element.SubFrameId ??= assembly.FindOwnerAt(
                    (double)element.X,
                    (double)element.Y)?.Id;
            }
        }

        return new ConstructionMemento
        {
            AssemblyData = assemblyData,
            Elements = payload.Elements,
            DocumentRect = payload.DocumentRect
        };
    }
}
