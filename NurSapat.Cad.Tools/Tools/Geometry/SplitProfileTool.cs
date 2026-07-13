namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Делит выбранный профиль (ячейку или импост) пополам перпендикулярным разрезом.
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
