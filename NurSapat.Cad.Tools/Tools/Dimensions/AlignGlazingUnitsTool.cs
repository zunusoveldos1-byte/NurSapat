namespace NurSapat.Cad.Tools.Tools.Dimensions;

/// <summary>
/// Автоматический расчёт створок под одинаковый размер стеклопакета.
/// TODO: пересчёт sash/glass cells в FrameNode.
/// </summary>
public sealed class AlignGlazingUnitsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AlignGlazingUnits;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    protected override bool TryExecute(CadToolContext context)
    {
        ReportNotImplemented(context, DisplayName);
        return false;
    }
}
