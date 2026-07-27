namespace NurSapat.Cad.Tools.Tools.Dimensions;

/// <summary>
/// Выравнивает выбранные стеклопакеты изменением положения разделяющих импостов.
/// </summary>
public sealed class AlignGlazingUnitsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AlignGlazingUnits;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryAlignGlazingUnits is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает выравнивание стеклопакетов");
            return false;
        }

        return context.TryAlignGlazingUnits();
    }
}
