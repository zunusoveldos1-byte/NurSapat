namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Смещение горизонтального импоста по оси Y.
/// TODO: drag selected horizontal impost + keyboard nudge.
/// </summary>
public sealed class OffsetImpostHorizontalTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.OffsetImpostHorizontal;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: выберите горизонтальный импост и переместите");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e) =>
        ReportNotImplemented(context, DisplayName);
}
