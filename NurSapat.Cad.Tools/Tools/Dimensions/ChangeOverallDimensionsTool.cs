namespace NurSapat.Cad.Tools.Tools.Dimensions;

/// <summary>
/// Ввод новых общих габаритов (ширина/высота) с пропорциональным масштабированием проёмов.
/// TODO: диалог размеров + масштабирование FrameRoot.
/// </summary>
public sealed class ChangeOverallDimensionsTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.ChangeOverallDimensions;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => false;

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TryChangeOverallDimensions?.Invoke() == true)
        {
            return true;
        }

        ReportNotImplemented(context, DisplayName);
        return false;
    }
}
