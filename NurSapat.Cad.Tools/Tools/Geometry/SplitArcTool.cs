namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>Разделяет арочную конструкцию вертикальным импостом в точке клика по дуге.</summary>
public sealed class SplitArcTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.SplitArc;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: кликните по арочному профилю в точке разделения");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        if (context.TrySplitArcAtPoint is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает разбиение арки");
            return;
        }

        if (!context.TrySplitArcAtPoint(e.WorldPoint))
        {
            context.SetStatus?.Invoke("Кликните по арочному профилю внутри конструкции");
        }
    }
}
