using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Snapping;

public readonly record struct SnapResult(CadPoint Point, SnapKind Kind, double Distance)
{
    public static SnapResult None(CadPoint fallback) => new(fallback, SnapKind.None, double.MaxValue);
}
