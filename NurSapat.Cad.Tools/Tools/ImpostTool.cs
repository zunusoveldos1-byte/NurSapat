using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.Tools.Tools;

public sealed class ImpostTool : CadToolBase
{
    private readonly SectionSplitOrientation _orientation;

    public override string Id { get; }

    public override string DisplayName { get; }

    public ImpostTool(string id, string displayName, SectionSplitOrientation orientation)
    {
        Id = id;
        DisplayName = displayName;
        _orientation = orientation;
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        context.HitTest(e.WorldPoint);
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        var section = context.HitTest(e.WorldPoint);
        if (section?.IsLeaf == true)
        {
            context.SplitSelected(_orientation, e.WorldPoint);
        }
    }
}
