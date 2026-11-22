namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Точка привязки фигуры к мировой сетке при создании через ПКМ.
/// Координаты X/Y — нижний левый угол bbox в системе CAD (Y вверх).
/// </summary>
public enum PlacementAnchor
{
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}
