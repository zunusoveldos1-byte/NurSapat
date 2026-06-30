namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Равномерное распределение импостов в проёме (например, деление на N равных частей).
/// TODO: uniform split via FrameNode.TrySplitUniformWithVolume.
/// </summary>
public sealed class AlignImpostsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AlignImposts;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    protected override bool TryExecute(CadToolContext context)
    {
        ReportNotImplemented(context, DisplayName);
        return false;
    }
}
