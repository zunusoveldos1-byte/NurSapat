using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.Tools.Tools;

public sealed class HardwareModifierTool : CadToolBase
{
    private readonly SashHardwareModifier _modifier;

    public override string Id { get; }

    public override string DisplayName { get; }

    public HardwareModifierTool(string id, string displayName, SashHardwareModifier modifier)
    {
        Id = id;
        DisplayName = displayName;
        _modifier = modifier;
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        context.HitTest(e.WorldPoint);
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        var section = context.HitTest(e.WorldPoint);
        if (section?.Fill.HasSash == true)
        {
            context.AddHardware(_modifier);
        }
    }
}
