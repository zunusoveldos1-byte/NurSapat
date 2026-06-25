namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Смещение вертикального импоста по оси X (мышь / стрелки).
/// TODO: drag selected vertical impost + keyboard nudge.
/// </summary>
public sealed class OffsetImpostVerticalTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.OffsetImpostVertical;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: выберите вертикальный импост и переместите");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e) =>
        ReportNotImplemented(context, DisplayName);
}
