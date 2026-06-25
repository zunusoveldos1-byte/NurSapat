namespace NurSapat.Cad.Tools.Tools.Selection;

public sealed class SelectProfileTool : SelectionModeCadToolBase
{
    public override CadToolKind Kind => CadToolKind.SelectProfile;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;
    protected override CadSelectionMode SelectionMode => CadSelectionMode.Profile;
}
