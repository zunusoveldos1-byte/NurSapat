using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

public static class ConstructionCloneHelper
{
    public static ConstructionMemento DeepClone(ConstructionMemento source, double offsetX = 0, double offsetY = 0)
    {
        var elementIdMap = new Dictionary<Guid, Guid>();
        var elements = source.Elements
            .Select(element =>
            {
                var clone = ConstructionMemento.CloneElement(element);
                var newId = Guid.NewGuid();
                elementIdMap[element.Id] = newId;
                clone.Id = newId;
                return clone;
            })
            .ToList();

        foreach (var element in elements)
        {
            if (element.HostSashId is Guid hostSashId && elementIdMap.TryGetValue(hostSashId, out var mappedSash))
            {
                element.HostSashId = mappedSash;
            }

            if (element.ParentGlazingId is Guid parentGlazingId
                && elementIdMap.TryGetValue(parentGlazingId, out var mappedGlazing))
            {
                element.ParentGlazingId = mappedGlazing;
            }
        }

        var nodeIdMap = new Dictionary<Guid, Guid>();
        var slotIdMap = new Dictionary<Guid, Guid>();
        var frameRoot = source.FrameRootSnapshot is null
            ? null
            : RemapTreeSnapshot(
                source.FrameRootSnapshot,
                nodeIdMap,
                slotIdMap,
                offsetX,
                offsetY);

        foreach (var element in elements)
        {
            if (element.HostFrameNodeId is Guid hostNodeId && nodeIdMap.TryGetValue(hostNodeId, out var mappedNode))
            {
                element.HostFrameNodeId = mappedNode;
            }

            if (element.ExtremeImpostSlotId is Guid slotId && slotIdMap.TryGetValue(slotId, out var mappedSlot))
            {
                element.ExtremeImpostSlotId = mappedSlot;
            }
        }

        CadRect? documentRect = source.DocumentRect;
        if (documentRect is CadRect rect
            && (Math.Abs(offsetX) > CadMath.Epsilon || Math.Abs(offsetY) > CadMath.Epsilon))
        {
            documentRect = new CadRect(
                rect.X + offsetX,
                rect.Y + offsetY,
                rect.Width,
                rect.Height);
        }

        return new ConstructionMemento
        {
            FrameRootSnapshot = frameRoot,
            Elements = elements,
            DocumentRect = documentRect
        };
    }

    private static FrameNode.TreeSnapshot RemapTreeSnapshot(
        FrameNode.TreeSnapshot source,
        Dictionary<Guid, Guid> nodeIdMap,
        Dictionary<Guid, Guid> slotIdMap,
        double offsetX,
        double offsetY)
    {
        var newNodeId = source.NodeId == Guid.Empty
            ? Guid.NewGuid()
            : nodeIdMap.TryGetValue(source.NodeId, out var existing)
                ? existing
                : nodeIdMap[source.NodeId] = Guid.NewGuid();

        return new FrameNode.TreeSnapshot
        {
            X = CadMath.RoundMm(source.X + offsetX),
            Y = CadMath.RoundMm(source.Y + offsetY),
            Width = source.Width,
            Height = source.Height,
            IsLeaf = source.IsLeaf,
            IsUniformSplit = source.IsUniformSplit,
            SplitOrientation = source.SplitOrientation,
            LineStyle = source.LineStyle,
            ImpostThicknessMm = source.ImpostThicknessMm,
            UniformImpostCount = source.UniformImpostCount,
            UniformCells = source.UniformCells
                .Select(cell => RemapTreeSnapshot(cell, nodeIdMap, slotIdMap, offsetX, offsetY))
                .ToList(),
            ChildA = source.ChildA is null
                ? null
                : RemapTreeSnapshot(source.ChildA, nodeIdMap, slotIdMap, offsetX, offsetY),
            ChildB = source.ChildB is null
                ? null
                : RemapTreeSnapshot(source.ChildB, nodeIdMap, slotIdMap, offsetX, offsetY),
            SplitRelativeCenter = source.SplitRelativeCenter,
            IsDummy = source.IsDummy,
            NodeId = newNodeId,
            ExtremeImposts = source.ExtremeImposts
                .Select(slot =>
                {
                    var newSlotId = slot.Id == Guid.Empty
                        ? Guid.NewGuid()
                        : slotIdMap.TryGetValue(slot.Id, out var existingSlot)
                            ? existingSlot
                            : slotIdMap[slot.Id] = Guid.NewGuid();
                    return new FrameNode.ExtremeImpostSlotSnapshot
                    {
                        Id = newSlotId,
                        Orientation = slot.Orientation,
                        EdgeAlignment = slot.EdgeAlignment,
                        ThicknessMm = slot.ThicknessMm,
                        OffsetMm = slot.OffsetMm,
                        SpanStartMm = slot.SpanStartMm,
                        SpanEndMm = slot.SpanEndMm,
                        LightOpeningInsetMm = slot.LightOpeningInsetMm
                    };
                })
                .ToList(),
            TopProfile = source.TopProfile is null
                ? null
                : new ProfileSegmentSnapshot
                {
                    SegmentType = source.TopProfile.SegmentType,
                    StartX = CadMath.RoundMm(source.TopProfile.StartX + offsetX),
                    StartY = CadMath.RoundMm(source.TopProfile.StartY + offsetY),
                    EndX = CadMath.RoundMm(source.TopProfile.EndX + offsetX),
                    EndY = CadMath.RoundMm(source.TopProfile.EndY + offsetY),
                    Radius = source.TopProfile.Radius,
                    ArcHeight = source.TopProfile.ArcHeight
                },
            Shape = source.Shape,
            Sill = source.Sill is null
                ? null
                : new FrameNode.SillElementSnapshot
                {
                    Id = Guid.NewGuid(),
                    WidthMm = source.Sill.WidthMm,
                    DepthMm = source.Sill.DepthMm,
                    ThicknessMm = source.Sill.ThicknessMm,
                    LeftOverhangMm = source.Sill.LeftOverhangMm,
                    RightOverhangMm = source.Sill.RightOverhangMm,
                    MaterialId = source.Sill.MaterialId,
                    Color = source.Sill.Color
                }
        };
    }
}
