namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Ввод точных координат X/Y для параметрически редактируемой вершины
/// (угол коробки, ось импоста, арочная управляющая вершина).
/// </summary>
public sealed class SetVertexCoordinatesTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.SetVertexCoordinates;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: выберите вершину, затем введите координаты");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        if (context.TrySetVertexCoordinates is not null)
        {
            if (!context.TrySetVertexCoordinates(e.WorldPoint))
            {
                context.SetStatus?.Invoke("Параметрическая вершина не найдена в точке клика");
            }

            return;
        }

        // Fallback: только диалог без применения (не должен вызываться при полной проводке).
        if (context.ShowVertexCoordinateDialog is null)
        {
            ReportNotImplemented(context, DisplayName);
            return;
        }

        var coords = context.ShowVertexCoordinateDialog(e.WorldPoint);
        if (coords is null)
        {
            return;
        }

        context.SetStatus?.Invoke($"Вершина: X={coords.Value.X:0}, Y={coords.Value.Y:0}");
    }
}
