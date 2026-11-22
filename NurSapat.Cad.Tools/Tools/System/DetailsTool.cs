namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Открывает read-only перечень элементов конструкции.</summary>
public sealed class DetailsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.Details;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryShowConstructionDetails is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает перечень деталей");
            return false;
        }

        return context.TryShowConstructionDetails();
    }
}
