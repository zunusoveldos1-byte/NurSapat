namespace NurSapat.Cad.Core.Snapping;

[Flags]
public enum SnapKind
{
    None = 0,
    Grid = 1,
    EndPoint = 2,
    MidPoint = 4,
    Intersection = 8,
    Object = EndPoint | MidPoint | Intersection
}
