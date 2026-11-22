namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Вставляет ранее скопированную конструкцию.</summary>
public sealed class PasteConstructionTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.PasteConstruction;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryPasteConstruction is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает вставку конструкции");
            return false;
        }

        return context.TryPasteConstruction();
    }
}
