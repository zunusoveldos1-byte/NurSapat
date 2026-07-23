using NurSapat.Cad.Tools.Tools;
using NurSapat.Cad.Tools.Tools.Dimensions;
using NurSapat.Cad.Tools.Tools.Geometry;
using NurSapat.Cad.Tools.Tools.Selection;
using NurSapat.Cad.Tools.Tools.System;

namespace NurSapat.Cad.Tools;

public sealed class CadToolFactory
{
    private readonly IReadOnlyDictionary<string, Func<ICadTool>> _creators;
    private readonly IReadOnlyDictionary<CadToolKind, Func<ICadTool>> _creatorsByKind;

    public IReadOnlyList<CadToolDescriptor> Tools { get; }

    public CadToolFactory()
    {
        var entries = new (CadToolKind Kind, Func<ICadTool> Create)[]
        {
            (CadToolKind.SelectElements, () => new SelectionTool()),
            (CadToolKind.DeleteLastPlaced, () => new DeleteLastPlacedTool()),
            (CadToolKind.Undo, () => new UndoTool()),
            (CadToolKind.SelectObject, () => new SelectObjectTool()),
            (CadToolKind.SelectProfile, () => new SelectProfileTool()),
            (CadToolKind.SelectVertex, () => new SelectVertexTool()),
            (CadToolKind.AlignGlazingUnits, () => new AlignGlazingUnitsTool()),
            (CadToolKind.AlignDeadLights, () => new AlignDeadLightsTool()),
            (CadToolKind.SetVertexCoordinates, () => new SetVertexCoordinatesTool()),
            (CadToolKind.SplitProfile, () => new SplitProfileTool()),
            (CadToolKind.OffsetImpostVertical, () => new OffsetImpostVerticalTool()),
            (CadToolKind.OffsetImpostHorizontal, () => new OffsetImpostHorizontalTool()),
            (CadToolKind.AlignImposts, () => new AlignImpostsTool()),
            (CadToolKind.ChangeOverallDimensions, () => new ChangeOverallDimensionsTool()),
            (CadToolKind.DeleteSelection, () => new DeleteSelectionTool()),
            (CadToolKind.ClearConstruction, () => new ClearConstructionTool()),
            (CadToolKind.RadiusVertex, () => new RadiusVertexTool()),
            (CadToolKind.Recalculate, () => new RecalculateTool()),
            (CadToolKind.Properties, () => new PropertiesTool()),
            (CadToolKind.Details, () => new DetailsTool()),
            (CadToolKind.ArcVertex, () => new ArcVertexTool()),
            (CadToolKind.RemoveArcVertex, () => new RemoveArcVertexTool()),
            (CadToolKind.SplitArc, () => new SplitArcTool()),
            (CadToolKind.SetRadius, () => new SetRadiusTool()),
            (CadToolKind.CopyConstruction, () => new CopyConstructionTool()),
            (CadToolKind.PasteConstruction, () => new PasteConstructionTool()),
            (CadToolKind.AddToStandard, () => new AddToStandardTool()),
            (CadToolKind.InsertFromStandard, () => new InsertFromStandardTool()),
        };

        _creatorsByKind = entries.ToDictionary(e => e.Kind, e => e.Create);

        var byId = entries.ToDictionary(
            e => CadToolMetadata.GetId(e.Kind),
            e => e.Create,
            StringComparer.OrdinalIgnoreCase);

        byId["select"] = () => new SelectionTool();
        byId["select-elements"] = () => new SelectionTool();
        byId["rectangle"] = () => new RectangleTool();
        byId["mullion-place-v"] = () => new MullionPlacementTool("mullion-place-v", "Импост вертикальный", MullionPlacementMode.Vertical);
        byId["mullion-place-h"] = () => new MullionPlacementTool("mullion-place-h", "Импост горизонтальный", MullionPlacementMode.Horizontal);
        byId["mullion-place-d"] = () => new MullionPlacementTool("mullion-place-d", "Импост наклонный", MullionPlacementMode.Diagonal);
        byId["design-rect"] = () => new DesignRectPlacementTool();

        _creators = byId;

        Tools = CadToolMetadata.All
            .Select(info => new CadToolDescriptor(
                info.Id,
                info.DisplayName,
                info.Kind,
                info.Group,
                info.Interaction,
                _creatorsByKind[info.Kind]))
            .ToList();
    }

    public IReadOnlyList<CadToolDescriptor> GetToolsByGroup(CadToolGroup group) =>
        Tools.Where(t => t.Group == group).ToList();

    public ICadTool Create(string id)
    {
        if (_creators.TryGetValue(id, out var create))
        {
            return create();
        }

        throw new InvalidOperationException($"CAD tool '{id}' is not registered.");
    }

    public ICadTool Create(CadToolKind kind)
    {
        if (kind == CadToolKind.None)
        {
            throw new InvalidOperationException("CadToolKind.None cannot be instantiated.");
        }

        if (_creatorsByKind.TryGetValue(kind, out var create))
        {
            return create();
        }

        throw new InvalidOperationException($"CAD tool kind '{kind}' is not registered.");
    }

    public CadToolKind? ResolveKind(string id) => CadToolMetadata.ResolveKind(id);
}

public sealed record CadToolDescriptor(
    string Id,
    string DisplayName,
    CadToolKind Kind,
    CadToolGroup Group,
    CadToolInteraction Interaction,
    Func<ICadTool> Create);
