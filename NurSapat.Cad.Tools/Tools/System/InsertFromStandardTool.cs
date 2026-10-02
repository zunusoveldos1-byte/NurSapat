namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Вставляет конструкцию из хранилища типовых шаблонов.</summary>
public sealed class InsertFromStandardTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.InsertFromStandard;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryInsertConstructionFromStandard is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает вставку из типовых конструкций");
            return false;
        }

        return context.TryInsertConstructionFromStandard();
    }
}
