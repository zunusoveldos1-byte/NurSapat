namespace NurSapat.Cad.Tools.Tools.Selection;

public sealed class SelectProfileTool : SelectionModeCadToolBase
{
    public override CadToolKind Kind => CadToolKind.SelectProfile;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    protected override CadSelectionMode SelectionMode => CadSelectionMode.Profile;

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        if (context.HandleConstructionClickSelection is { } applyClick)
        {
            applyClick(e.WorldPoint, e.IsCtrlDown);
            context.Invalidate?.Invoke();
            return;
        }

        base.OnMouseDown(context, e);
    }
}
