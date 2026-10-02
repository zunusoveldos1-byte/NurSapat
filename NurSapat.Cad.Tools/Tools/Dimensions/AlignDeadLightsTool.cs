namespace NurSapat.Cad.Tools.Tools.Dimensions;

/// <summary>
/// Выравнивает выбранные глухие световые проёмы «в свету».
/// </summary>
public sealed class AlignDeadLightsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AlignDeadLights;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryAlignDeadLights is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает выравнивание глухих просветов");
            return false;
        }

        return context.TryAlignDeadLights();
    }
}
