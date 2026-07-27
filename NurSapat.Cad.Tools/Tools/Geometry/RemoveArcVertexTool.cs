namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>Удаляет дуговую вершину, возвращая верхний профиль к прямой линии.</summary>
public sealed class RemoveArcVertexTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.RemoveArcVertex;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryRemoveArcVertex is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает удаление дуговых вершин");
            return false;
        }

        return context.TryRemoveArcVertex();
    }
}
