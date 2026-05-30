namespace NurSapat.Cad.Tools.Tools;

public sealed class FrameTool : CadToolBase
{
    private readonly bool _hiddenProfile;

    public override string Id { get; }

    public override string DisplayName { get; }

    public FrameTool(string id, string displayName, bool hiddenProfile = false)
    {
        Id = id;
        DisplayName = displayName;
        _hiddenProfile = hiddenProfile;
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        var options = context.Document.Structure.Options;
        context.InitializeFrame(options.Width, options.Height, _hiddenProfile);
    }
}
