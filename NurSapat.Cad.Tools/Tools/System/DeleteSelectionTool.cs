namespace NurSapat.Cad.Tools.Tools.System;

public sealed class DeleteSelectionTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.DeleteSelection;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryDeleteSelection?.Invoke() == true)
        {
            return true;
        }

        context.SetStatus?.Invoke("Нет выбранного объекта для удаления");
        return false;
    }
}
