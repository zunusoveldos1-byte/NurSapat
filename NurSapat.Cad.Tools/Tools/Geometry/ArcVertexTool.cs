namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>Выделение и редактирование дуговых вершин арочного профиля.</summary>
public sealed class ArcVertexTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.ArcVertex;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: кликните по верхнему профилю или перетащите маркер дуги");

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        if (context.TryPressArcVertex?.Invoke(e.WorldPoint) != true)
        {
            context.SetStatus?.Invoke("Кликните по верхнему профилю или маркеру дуги");
        }
    }
}
