using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;
using System.Text.Json;

namespace NurSapat.App.Services.Documents;

public static class CadProjectSnapshotHelper
{
    public static CadProjectData Capture(CadEditorViewModel.CadEditorSnapshot source) => new()
    {
        DocumentName = source.DocumentName,
        ViewportZoomFactor = source.ViewportZoomFactor,
        ViewportPanX = source.ViewportPanX,
        ViewportPanY = source.ViewportPanY,
        BaseStrokeColor = source.BaseStrokeColor,
        FrameTreeJson = source.FrameTree is null
            ? null
            : JsonSerializer.Serialize(source.FrameTree),
        FrameRoot = source.FrameRoot is null ? null : ToFrameData(source.FrameRoot)
    };

    public static CadEditorViewModel.CadEditorSnapshot ToSnapshot(CadProjectData data) => new()
    {
        DocumentName = data.DocumentName,
        ViewportZoomFactor = data.ViewportZoomFactor,
        ViewportPanX = data.ViewportPanX,
        ViewportPanY = data.ViewportPanY,
        BaseStrokeColor = data.BaseStrokeColor,
        FrameTree = string.IsNullOrWhiteSpace(data.FrameTreeJson)
            ? null
            : JsonSerializer.Deserialize<FrameNode.TreeSnapshot>(data.FrameTreeJson),
        FrameRoot = data.FrameRoot is null ? null : FromFrameData(data.FrameRoot)
    };

    private static FrameNodeData ToFrameData(CadEditorViewModel.FrameNodeSnapshot node) => new()
    {
        X = node.X,
        Y = node.Y,
        Width = node.Width,
        Height = node.Height,
        IsLeaf = node.IsLeaf,
        IsUniformSplit = node.IsUniformSplit,
        IsDummy = node.IsDummy,
        SplitOrientation = node.SplitOrientation,
        LineStyle = node.LineStyle,
        ImpostThicknessMm = node.ImpostThicknessMm,
        UniformImpostCount = node.UniformImpostCount,
        UniformCells = node.UniformCells.Select(ToFrameData).ToList(),
        ChildA = node.ChildA is null ? null : ToFrameData(node.ChildA),
        ChildB = node.ChildB is null ? null : ToFrameData(node.ChildB),
        TopProfile = node.TopProfile is null ? null : ToProfileData(node.TopProfile)
    };

    private static CadEditorViewModel.FrameNodeSnapshot FromFrameData(FrameNodeData data) => new()
    {
        X = data.X,
        Y = data.Y,
        Width = data.Width,
        Height = data.Height,
        IsLeaf = data.IsLeaf,
        IsUniformSplit = data.IsUniformSplit,
        IsDummy = data.IsDummy,
        SplitOrientation = data.SplitOrientation,
        LineStyle = data.LineStyle,
        ImpostThicknessMm = data.ImpostThicknessMm,
        UniformImpostCount = data.UniformImpostCount,
        UniformCells = data.UniformCells.Select(FromFrameData).ToList(),
        ChildA = data.ChildA is null ? null : FromFrameData(data.ChildA),
        ChildB = data.ChildB is null ? null : FromFrameData(data.ChildB),
        TopProfile = data.TopProfile is null ? null : FromProfileData(data.TopProfile)
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
        SegmentType = Enum.TryParse<SegmentType>(data.SegmentType, ignoreCase: true, out var parsed)
            ? parsed
            : SegmentType.Line,
        StartX = data.StartX,
        StartY = data.StartY,
        EndX = data.EndX,
        EndY = data.EndY,
        Radius = data.Radius,
        ArcHeight = data.ArcHeight
    };
}
