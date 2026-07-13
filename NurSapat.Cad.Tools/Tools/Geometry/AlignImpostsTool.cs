namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Восстанавливает равномерный шаг уже существующих импостов.
/// </summary>
public sealed class AlignImpostsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AlignImposts;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryAlignImposts is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает выравнивание импостов");
            return false;
        }

        return context.TryAlignImposts();
    }
}
