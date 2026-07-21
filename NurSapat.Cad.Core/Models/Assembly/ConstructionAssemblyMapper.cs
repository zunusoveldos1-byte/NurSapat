using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Сериализация ConstructionAssembly ↔ DTO v3.
/// </summary>
public static class ConstructionAssemblyMapper
{
    public static ConstructionAssemblyData ToData(ConstructionAssembly assembly) => new()
    {
        GlobalFrame = new GlobalFrameData
        {
            X = assembly.GlobalFrame.X,
            Y = assembly.GlobalFrame.Y,
            Width = assembly.GlobalFrame.Width,
            Height = assembly.GlobalFrame.Height
        },
        TransitionProfiles = assembly.TransitionProfiles.Select(ToTransitionData).ToList(),
        SubFrames = assembly.SubFrames.Select(ToSubFrameData).ToList()
    };

    public static ConstructionAssembly FromData(ConstructionAssemblyData data)
    {
        if (data.GlobalFrame is null || data.SubFrames.Count == 0)
        {
            throw new InvalidOperationException(
                "Schema v3 Assembly data must contain GlobalFrame and at least one SubFrame.");
        }

        var global = GlobalFrame.FromBounds(
            data.GlobalFrame.X,
            data.GlobalFrame.Y,
            data.GlobalFrame.Width,
            data.GlobalFrame.Height);

        var subFrames = data.SubFrames
            .OrderBy(s => s.OrderIndex)
            .Select(FromSubFrameData)
            .ToList();

        var transitions = data.TransitionProfiles.Select(FromTransitionData).ToList();

        return new ConstructionAssembly
        {
            GlobalFrame = global,
            TransitionProfiles = transitions,
            SubFrames = subFrames
        };
    }

    private static TransitionProfileData ToTransitionData(TransitionProfileModel model) => new()
    {
        Id = model.Id,
        PartitionNodeId = model.PartitionNodeId,
        Orientation = model.Orientation.ToString(),
        AxisCoordinate = model.AxisCoordinate,
        ThicknessMm = model.ThicknessMm,
        MaterialId = model.MaterialId,
        HostGridLineId = model.HostGridLineId,
        LeftSubFrameId = model.LeftSubFrameId,
        RightSubFrameId = model.RightSubFrameId
    };

    private static TransitionProfileModel FromTransitionData(TransitionProfileData data) => new()
    {
        Id = data.Id == Guid.Empty ? Guid.NewGuid() : data.Id,
        PartitionNodeId = data.PartitionNodeId,
        Orientation = Enum.TryParse<GridLineOrientation>(data.Orientation, true, out var orientation)
            ? orientation
            : GridLineOrientation.Vertical,
        AxisCoordinate = data.AxisCoordinate,
        ThicknessMm = data.ThicknessMm,
        MaterialId = data.MaterialId,
        HostGridLineId = data.HostGridLineId,
        LeftSubFrameId = data.LeftSubFrameId,
        RightSubFrameId = data.RightSubFrameId
    };

    private static SubFrameData ToSubFrameData(SubFrame subFrame) => new()
    {
        Id = subFrame.Id,
        OriginWorldX = subFrame.OriginWorldX,
        OriginWorldY = subFrame.OriginWorldY,
        OuterWidth = subFrame.OuterWidth,
        OuterHeight = subFrame.OuterHeight,
        TransitionCoverStripMm = subFrame.TransitionCoverStripMm,
        OuterCoverStripMm = subFrame.OuterCoverStripMm,
        OrderIndex = subFrame.OrderIndex,
        FrameRoot = FrameNodeMapper.ToData(subFrame.Root)
    };

    private static SubFrame FromSubFrameData(SubFrameData data)
    {
        if (data.FrameRoot is null)
        {
            throw new InvalidDataException(
                $"SubFrame {data.Id} has no local FrameNode tree.");
        }

        var root = FrameNodeMapper.FromData(data.FrameRoot)
            ?? throw new InvalidDataException(
                $"SubFrame {data.Id} local FrameNode tree is invalid.");

        var subFrameId = data.Id == Guid.Empty ? Guid.NewGuid() : data.Id;
        root.AssignOwnerSubFrame(subFrameId);
        return new SubFrame
        {
            Id = subFrameId,
            OriginWorldX = data.OriginWorldX,
            OriginWorldY = data.OriginWorldY,
            OuterWidth = data.OuterWidth,
            OuterHeight = data.OuterHeight,
            TransitionCoverStripMm = data.TransitionCoverStripMm,
            OuterCoverStripMm = data.OuterCoverStripMm,
            OrderIndex = data.OrderIndex,
            Root = root
        };
    }
}

/// <summary>Маппинг FrameNode ↔ FrameNodeData для подрам.</summary>
public static class FrameNodeMapper
{
    public static FrameNodeData ToData(FrameNode node) =>
        FromTreeSnapshot(node.CaptureTreeSnapshot());

    public static FrameNode? FromData(FrameNodeData data) =>
        FrameNode.RestoreTree(ToTreeSnapshot(data));

    private static FrameNodeData FromTreeSnapshot(FrameNode.TreeSnapshot snapshot) => new()
    {
        X = snapshot.X,
        Y = snapshot.Y,
        Width = snapshot.Width,
        Height = snapshot.Height,
        IsLeaf = snapshot.IsLeaf,
        IsUniformSplit = snapshot.IsUniformSplit,
        IsDummy = snapshot.IsDummy,
        SplitOrientation = snapshot.SplitOrientation.ToString(),
        LineStyle = snapshot.LineStyle.ToString(),
        ImpostThicknessMm = snapshot.ImpostThicknessMm,
        UniformImpostCount = snapshot.UniformImpostCount,
        UniformCells = snapshot.UniformCells.Select(FromTreeSnapshot).ToList(),
        ChildA = snapshot.ChildA is null ? null : FromTreeSnapshot(snapshot.ChildA),
        ChildB = snapshot.ChildB is null ? null : FromTreeSnapshot(snapshot.ChildB),
        NodeId = snapshot.NodeId,
        SplitRelativeCenter = snapshot.SplitRelativeCenter,
        SeparatorKind = snapshot.SeparatorKind.ToString(),
        TopProfile = snapshot.TopProfile is null ? null : ToProfileData(snapshot.TopProfile),
        Shape = snapshot.Shape.ToString(),
        Sill = snapshot.Sill is null ? null : ToSillData(snapshot.Sill),
        ExtremeImposts = snapshot.ExtremeImposts.Select(ToExtremeData).ToList()
    };

    private static FrameNode.TreeSnapshot ToTreeSnapshot(FrameNodeData data) => new()
    {
        X = data.X,
        Y = data.Y,
        Width = data.Width,
        Height = data.Height,
        IsLeaf = data.IsLeaf,
        IsUniformSplit = data.IsUniformSplit,
        IsDummy = data.IsDummy,
        SplitOrientation = Enum.TryParse<SplitOrientation>(data.SplitOrientation, true, out var split)
            ? split
            : SplitOrientation.None,
        LineStyle = Enum.TryParse<ImpostLineStyle>(data.LineStyle, true, out var lineStyle)
            ? lineStyle
            : ImpostLineStyle.Dashed,
        ImpostThicknessMm = data.ImpostThicknessMm,
        UniformImpostCount = data.UniformImpostCount,
        UniformCells = data.UniformCells.Select(ToTreeSnapshot).ToList(),
        ChildA = data.ChildA is null ? null : ToTreeSnapshot(data.ChildA),
        ChildB = data.ChildB is null ? null : ToTreeSnapshot(data.ChildB),
        NodeId = data.NodeId,
        SplitRelativeCenter = data.SplitRelativeCenter,
        SeparatorKind = Enum.TryParse<SeparatorKind>(data.SeparatorKind, true, out var separator)
            ? separator
            : SeparatorKind.None,
        TopProfile = data.TopProfile is null ? null : FromProfileData(data.TopProfile),
        Shape = Enum.TryParse<ConstructionShape>(data.Shape, true, out var shape)
            ? shape
            : ConstructionShape.Rectangle,
        Sill = data.Sill is null ? null : FromSillData(data.Sill),
        ExtremeImposts = data.ExtremeImposts.Select(FromExtremeData).ToList()
    };

    private static ProfileSegmentData ToProfileData(ProfileSegmentSnapshot snapshot) => new()
    {
        SegmentType = snapshot.SegmentType.ToString(),
        StartX = snapshot.StartX,
        StartY = snapshot.StartY,
        EndX = snapshot.EndX,
        EndY = snapshot.EndY,
        Radius = snapshot.Radius,
        ArcHeight = snapshot.ArcHeight
    };

    private static ProfileSegmentSnapshot FromProfileData(ProfileSegmentData data) => new()
    {
        SegmentType = Enum.TryParse<SegmentType>(data.SegmentType, true, out var segmentType)
            ? segmentType
            : SegmentType.Line,
        StartX = data.StartX,
        StartY = data.StartY,
        EndX = data.EndX,
        EndY = data.EndY,
        Radius = data.Radius,
        ArcHeight = data.ArcHeight
    };

    private static SillElementData ToSillData(FrameNode.SillElementSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        WidthMm = snapshot.WidthMm,
        DepthMm = snapshot.DepthMm,
        ThicknessMm = snapshot.ThicknessMm,
        LeftOverhangMm = snapshot.LeftOverhangMm,
        RightOverhangMm = snapshot.RightOverhangMm,
        MaterialId = snapshot.MaterialId,
        Color = snapshot.Color
    };

    private static FrameNode.SillElementSnapshot FromSillData(SillElementData data) => new()
    {
        Id = data.Id,
        WidthMm = data.WidthMm,
        DepthMm = data.DepthMm,
        ThicknessMm = data.ThicknessMm,
        LeftOverhangMm = data.LeftOverhangMm,
        RightOverhangMm = data.RightOverhangMm,
        MaterialId = data.MaterialId,
        Color = data.Color
    };

    private static ExtremeImpostSlotData ToExtremeData(FrameNode.ExtremeImpostSlotSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Orientation = snapshot.Orientation.ToString(),
        EdgeAlignment = snapshot.EdgeAlignment.ToString(),
        OffsetMm = snapshot.OffsetMm,
        ThicknessMm = snapshot.ThicknessMm,
        SpanStartMm = snapshot.SpanStartMm,
        SpanEndMm = snapshot.SpanEndMm,
        LightOpeningInsetMm = snapshot.LightOpeningInsetMm
    };

    private static FrameNode.ExtremeImpostSlotSnapshot FromExtremeData(ExtremeImpostSlotData data) => new()
    {
        Id = data.Id,
        Orientation = Enum.TryParse<SplitOrientation>(data.Orientation, true, out var orientation)
            ? orientation
            : SplitOrientation.None,
        EdgeAlignment = Enum.TryParse<ImpostEdgeAlignment>(data.EdgeAlignment, true, out var alignment)
            ? alignment
            : ImpostEdgeAlignment.Left,
        OffsetMm = data.OffsetMm,
        ThicknessMm = data.ThicknessMm,
        SpanStartMm = data.SpanStartMm,
        SpanEndMm = data.SpanEndMm,
        LightOpeningInsetMm = data.LightOpeningInsetMm
    };
}
