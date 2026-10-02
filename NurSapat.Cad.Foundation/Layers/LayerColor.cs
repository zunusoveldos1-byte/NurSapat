namespace NurSapat.Cad.Foundation.Layers;

/// <summary>
/// Цвет слоя (упрощённое значение ядра; UI интерпретирует его в реальный цвет темы).
/// </summary>
public readonly record struct LayerColor(byte R, byte G, byte B, byte A = 255)
{
    public static LayerColor Default { get; } = new(220, 220, 220);
    public static LayerColor White { get; } = new(255, 255, 255);
    public static LayerColor Black { get; } = new(0, 0, 0);
    public static LayerColor Red { get; } = new(220, 60, 60);
    public static LayerColor Green { get; } = new(60, 200, 90);
    public static LayerColor Blue { get; } = new(60, 120, 220);
    public static LayerColor Yellow { get; } = new(230, 200, 40);
}
