namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Открывает диалог свойств выбранного объекта.</summary>
public sealed class PropertiesTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.Properties;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryShowObjectProperties is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает свойства объекта");
            return false;
        }

        return context.TryShowObjectProperties();
    }
}
