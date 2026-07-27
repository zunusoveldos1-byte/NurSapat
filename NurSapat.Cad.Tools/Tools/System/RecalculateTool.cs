namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Полный пересчёт геометрии конструкции и связанной спецификации.</summary>
public sealed class RecalculateTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.Recalculate;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryRecalculateConstruction is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает пересчёт конструкции");
            return false;
        }

        return context.TryRecalculateConstruction();
    }
}
