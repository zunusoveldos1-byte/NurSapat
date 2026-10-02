namespace NurSapat.Cad.Tools.Tools.System;

/// <summary>Сохраняет текущую конструкцию в хранилище типовых шаблонов.</summary>
public sealed class AddToStandardTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AddToStandard;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryAddConstructionToStandard is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает сохранение в типовые конструкции");
            return false;
        }

        return context.TryAddConstructionToStandard();
    }
}
