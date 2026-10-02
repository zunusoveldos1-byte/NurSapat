namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Выделение радиусных вершин: подсветка верхнего профиля и преобразование отрезка в дугу.
/// </summary>
public sealed class RadiusVertexTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.RadiusVertex;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: наведите на верхний профиль и кликните ЛКМ");
}
