namespace NurSapat.Cad.Foundation.Snapping;

/// <summary>
/// Настройки snapping-подсистемы.
/// </summary>
public sealed class SnapSettings
{
    public SnapKind EnabledKinds { get; set; } = SnapKind.Grid | SnapKind.Object;
    public bool EnableGridSnap { get; set; } = true;
    public bool EnableObjectSnap { get; set; } = true;
    public double GridStep { get; set; } = 10.0;
    public double Tolerance { get; set; } = 5.0;
}
