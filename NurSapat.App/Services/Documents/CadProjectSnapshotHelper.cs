using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services.Documents;

public static class CadProjectSnapshotHelper
{
    public static CadProjectData Capture(CadEditorViewModel.CadEditorSnapshot source) => new()
    {
        DocumentName = source.DocumentName,
        ViewportZoomFactor = source.ViewportZoomFactor,
        ViewportPanX = source.ViewportPanX,
        ViewportPanY = source.ViewportPanY,
        FrameRoot = source.FrameRoot is null ? null : ToFrameData(source.FrameRoot)
    };

    public static CadEditorViewModel.CadEditorSnapshot ToSnapshot(CadProjectData data) => new()
    {
        DocumentName = data.DocumentName,
        ViewportZoomFactor = data.ViewportZoomFactor,
        ViewportPanX = data.ViewportPanX,
        ViewportPanY = data.ViewportPanY,
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
        SplitOrientation = node.SplitOrientation,
        LineStyle = node.LineStyle,
        ImpostThicknessMm = node.ImpostThicknessMm,
        UniformImpostCount = node.UniformImpostCount,
        UniformCells = node.UniformCells.Select(ToFrameData).ToList(),
        ChildA = node.ChildA is null ? null : ToFrameData(node.ChildA),
        ChildB = node.ChildB is null ? null : ToFrameData(node.ChildB)
    };

    private static CadEditorViewModel.FrameNodeSnapshot FromFrameData(FrameNodeData data) => new()
    {
        X = data.X,
        Y = data.Y,
        Width = data.Width,
        Height = data.Height,
        IsLeaf = data.IsLeaf,
        IsUniformSplit = data.IsUniformSplit,
        SplitOrientation = data.SplitOrientation,
        LineStyle = data.LineStyle,
        ImpostThicknessMm = data.ImpostThicknessMm,
        UniformImpostCount = data.UniformImpostCount,
        UniformCells = data.UniformCells.Select(FromFrameData).ToList(),
        ChildA = data.ChildA is null ? null : FromFrameData(data.ChildA),
        ChildB = data.ChildB is null ? null : FromFrameData(data.ChildB)
    };
}
