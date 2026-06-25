namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Ввод точных координат X/Y для выбранной вершины.
/// TODO: диалог координат + привязка к FrameNode/OpeningNode.
/// </summary>
public sealed class SetVertexCoordinatesTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.SetVertexCoordinates;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: выберите вершину, затем введите координаты");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        if (context.ShowVertexCoordinateDialog == null)
        {
            ReportNotImplemented(context, DisplayName);
            return;
        }

        var coords = context.ShowVertexCoordinateDialog(e.WorldPoint);
        if (coords == null)
        {
            return;
        }

        context.SetStatus?.Invoke($"Вершина: X={coords.Value.X:0}, Y={coords.Value.Y:0} — применение в разработке");
    }
}
