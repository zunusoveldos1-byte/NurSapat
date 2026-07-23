namespace NurSapat.Cad.Tools.Tools.System;

public sealed class DeleteLastPlacedTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.DeleteLastPlaced;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    protected override bool TryExecute(CadToolContext context) =>
        context.TryDeleteLastPlaced?.Invoke() == true;
}
