namespace NurSapat.Cad.Tools.Tools.System;

public sealed class ClearConstructionTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.ClearConstruction;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryClearConstruction?.Invoke() == true)
        {
            return true;
        }

        context.SetStatus?.Invoke("Нет конструкции для очистки");
        return false;
    }
}
