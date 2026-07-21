using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Assembly;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

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
        var assemblyData = CloneAssemblyData(
            source.AssemblyData,
            elementIdMap,
            nodeIdMap,
            slotIdMap,
            offsetX,
            offsetY,
            out var subFrameIdMap,
            out var gridLineIdMap);

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

            if (element.SubFrameId is Guid subFrameId
                && subFrameIdMap.TryGetValue(subFrameId, out var mappedSubFrame))
            {
                element.SubFrameId = mappedSubFrame;
            }

            if (element.HostGridLineId is Guid gridLineId
                && gridLineIdMap.TryGetValue(gridLineId, out var mappedGridLine))
            {
                element.HostGridLineId = mappedGridLine;
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
            AssemblyData = assemblyData,
            Elements = elements,
            DocumentRect = documentRect
        };
    }

    private static ConstructionAssemblyData? CloneAssemblyData(
        ConstructionAssemblyData? source,
        IReadOnlyDictionary<Guid, Guid> elementIdMap,
        Dictionary<Guid, Guid> nodeIdMap,
        Dictionary<Guid, Guid> slotIdMap,
        double offsetX,
        double offsetY,
        out Dictionary<Guid, Guid> subFrameIdMap,
        out Dictionary<Guid, Guid> gridLineIdMap)
    {
        subFrameIdMap = new Dictionary<Guid, Guid>();
        gridLineIdMap = new Dictionary<Guid, Guid>();
        if (source?.GlobalFrame is null)
        {
            return null;
        }

        var copy = ConstructionAssemblyMapper.ToData(
            ConstructionAssemblyMapper.FromData(source));
        copy.GlobalFrame!.X = CadMath.RoundMm(copy.GlobalFrame.X + offsetX);
        copy.GlobalFrame.Y = CadMath.RoundMm(copy.GlobalFrame.Y + offsetY);

        foreach (var subFrame in copy.SubFrames)
        {
            var oldId = subFrame.Id;
            var newId = Guid.NewGuid();
            subFrameIdMap[oldId] = newId;
            subFrame.Id = newId;
            subFrame.OriginWorldX = CadMath.RoundMm(subFrame.OriginWorldX + offsetX);
            subFrame.OriginWorldY = CadMath.RoundMm(subFrame.OriginWorldY + offsetY);

            if (subFrame.FrameRoot is not null
                && FrameNodeMapper.FromData(subFrame.FrameRoot) is FrameNode localRoot)
            {
                var remapped = RemapTreeSnapshot(
                    localRoot.CaptureTreeSnapshot(),
                    nodeIdMap,
                    slotIdMap,
                    0,
                    0);
                subFrame.FrameRoot = FrameNodeMapper.ToData(FrameNode.RestoreTree(remapped)!);
            }
        }

        foreach (var transition in copy.TransitionProfiles)
        {
            if (elementIdMap.TryGetValue(transition.Id, out var mappedTransition))
            {
                transition.Id = mappedTransition;
            }

            transition.AxisCoordinate = CadMath.RoundMm(
                transition.AxisCoordinate
                + (string.Equals(transition.Orientation, "Horizontal", StringComparison.OrdinalIgnoreCase)
                    ? offsetY
                    : offsetX));
            if (transition.LeftSubFrameId is Guid left
                && subFrameIdMap.TryGetValue(left, out var mappedLeft))
            {
                transition.LeftSubFrameId = mappedLeft;
            }
            if (transition.RightSubFrameId is Guid right
                && subFrameIdMap.TryGetValue(right, out var mappedRight))
            {
                transition.RightSubFrameId = mappedRight;
            }
            if (transition.PartitionNodeId is Guid partition
                && nodeIdMap.TryGetValue(partition, out var mappedPartition))
            {
                transition.PartitionNodeId = mappedPartition;
            }
        }

        var sourceAssembly = ConstructionAssemblyMapper.FromData(source);
        var clonedAssembly = ConstructionAssemblyMapper.FromData(copy);
        foreach (var sourceOwner in sourceAssembly.SubFrames)
        {
            if (!subFrameIdMap.TryGetValue(sourceOwner.Id, out var clonedOwnerId)
                || clonedAssembly.SubFrames.FirstOrDefault(owner => owner.Id == clonedOwnerId)
                    is not { } clonedOwner)
            {
                continue;
            }

            var clonedLines = FrameNodeGridLineProjector.Project(clonedOwner.Root).All;
            foreach (var sourceLine in FrameNodeGridLineProjector.Project(sourceOwner.Root).All)
            {
                if (sourceLine.SourceNodeId is not Guid sourceNodeId
                    || !nodeIdMap.TryGetValue(sourceNodeId, out var clonedNodeId))
                {
                    continue;
                }

                var clonedLine = clonedLines.FirstOrDefault(candidate =>
                    candidate.SourceNodeId == clonedNodeId
                    && candidate.Orientation == sourceLine.Orientation
                    && candidate.UniformGapIndex == sourceLine.UniformGapIndex);
                if (clonedLine is not null)
                {
                    gridLineIdMap[sourceLine.Id.Value] = clonedLine.Id.Value;
                }
            }
        }

        return copy;
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
            SeparatorKind = source.SeparatorKind,
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
