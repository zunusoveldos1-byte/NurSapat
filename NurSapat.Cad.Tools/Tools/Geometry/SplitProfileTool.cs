namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// «Разбить профиль пополам»: мгновенное действие тулбара.
/// Делит выбранный профиль рамы (или ячейку/импост) перпендикулярным разрезом
/// через середину; стык моделируется импостом с <c>SplitRelativeCenter = 0.5</c>.
/// </summary>
public sealed class SplitProfileTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.SplitProfile;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TrySplitProfile is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает разбиение профиля");
            return false;
        }

        return context.TrySplitProfile();
    }
}
