namespace NurSapat.Cad.Tools.Tools.System;

public sealed class UndoTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.Undo;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryUndo?.Invoke() == true)
        {
            return true;
        }

        if (context.CommandManager.CanUndo)
        {
            context.CommandManager.Undo();
            context.SetStatus?.Invoke("Действие отменено");
            return true;
        }

        context.SetStatus?.Invoke("Нечего отменять");
        return false;
    }
}
