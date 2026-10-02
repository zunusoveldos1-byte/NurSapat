namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Копирует текущую конструкцию в буфер обмена редактора.</summary>
public sealed class CopyConstructionTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.CopyConstruction;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryCopyConstruction is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает копирование конструкции");
            return false;
        }

        return context.TryCopyConstruction();
    }
}
