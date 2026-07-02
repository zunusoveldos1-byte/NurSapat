namespace NurSapat.Cad.Tools.Tools.Selection;

public sealed class SelectObjectTool : SelectionModeCadToolBase
{
    public override CadToolKind Kind => CadToolKind.SelectObject;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;
    protected override CadSelectionMode SelectionMode => CadSelectionMode.Object;
}
