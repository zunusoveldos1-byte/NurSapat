using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.Tools.Tools;

public sealed class FillTool : CadToolBase
{
    private readonly SectionFillKind _fillKind;

    public override string Id { get; }

    public override string DisplayName { get; }

    public FillTool(string id, string displayName, SectionFillKind fillKind)
    {
        Id = id;
        DisplayName = displayName;
        _fillKind = fillKind;
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
            context.ApplyFill(_fillKind);
        }
    }
}
