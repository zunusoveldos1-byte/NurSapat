using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools;

public enum DesignPlacementKind
{
    MullionLine,
    AreaRect,
    Point
}

public sealed record DesignPlacementCommit(
    string ToolId,
    string Label,
    DesignPlacementKind Kind,
    CadPoint Anchor,
    CadPoint? End = null,
    CadRect? Rect = null);
