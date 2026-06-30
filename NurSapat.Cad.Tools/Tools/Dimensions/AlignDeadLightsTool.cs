namespace NurSapat.Cad.Tools.Tools.Dimensions;

/// <summary>
/// Выравнивание размеров световых проёмов «в свету».
/// TODO: equalize leaf opening widths/heights in FrameNode tree.
/// </summary>
public sealed class AlignDeadLightsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.AlignDeadLights;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    protected override bool TryExecute(CadToolContext context)
    {
        ReportNotImplemented(context, DisplayName);
        return false;
    }
}
