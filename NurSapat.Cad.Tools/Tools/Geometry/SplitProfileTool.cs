namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Физически делит выбранную рейку профиля рамы пополам (стык 90°).
/// Не вставляет импост в световой проём.
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
