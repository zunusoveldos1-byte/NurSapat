namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Разделение существующего импоста или стороны рамы новой вершиной.
/// TODO: split FrameNode at click position.
/// </summary>
public sealed class SplitProfileTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.SplitProfile;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: кликните по профилю в точке разделения");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e) =>
        ReportNotImplemented(context, DisplayName);
}
