using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Смещение вертикального импоста: выделение на холсте, призрак при перетаскивании, фиксация при отпускании ЛКМ.
/// </summary>
public sealed class OffsetImpostVerticalTool : CadToolBase
{
    public override CadToolKind Kind => CadToolKind.OffsetImpostVertical;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: ЛКМ по импосту — выделить и перетащить");
}
