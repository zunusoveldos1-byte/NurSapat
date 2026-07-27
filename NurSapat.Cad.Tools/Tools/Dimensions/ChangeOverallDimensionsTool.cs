namespace NurSapat.Cad.Tools.Tools.Dimensions;

/// <summary>
/// Ввод новых общих габаритов (ширина/высота) с пропорциональным масштабированием проёмов.
/// </summary>
public sealed class ChangeOverallDimensionsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.ChangeOverallDimensions;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryChangeOverallDimensions is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает изменение габаритов");
            return false;
        }

        return context.TryChangeOverallDimensions();
    }
}
