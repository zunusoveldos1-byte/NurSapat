namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>Задаёт радиус выбранной арочной дуги.</summary>
public sealed class SetRadiusTool : InstantCadToolBase
{
    public override CadToolKind Kind => CadToolKind.SetRadius;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);

    protected override bool TryExecute(CadToolContext context)
    {
        if (context.TrySetArcRadius is null)
        {
            context.SetStatus?.Invoke("Редактор не поддерживает задание радиуса");
            return false;
        }

        return context.TrySetArcRadius();
    }
}
