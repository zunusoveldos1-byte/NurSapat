namespace NurSapat.Cad.Core.Snapping;

public readonly record struct CadSnapTick(double Coordinate, CadSnapTickAxis Axis, string Source);

public enum CadSnapTickAxis
{
    X,
    Y
}
