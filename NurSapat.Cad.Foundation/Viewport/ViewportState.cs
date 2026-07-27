using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Viewport;

/// <summary>
/// Неизменяемый снимок состояния <see cref="IViewport"/> для undo/redo
/// изменений вида и для сохранения/восстановления сессии.
/// </summary>
public readonly record struct ViewportState
(
    double Zoom,
    Point2D Pan,
    double Rotation,
    double Dpi,
    Size ViewportSize
)
{
    public static ViewportState Default { get; } = new(1.0, Point2D.Zero, 0, 96.0, new Size(1000, 700));

    /// <summary>Применить снимок к камере.</summary>
    public void ApplyTo(Camera2D camera)
    {
        camera.Zoom = Zoom;
        camera.Pan = Pan;
        camera.Rotation = Rotation;
        camera.Dpi = Dpi;
        camera.ViewportSize = ViewportSize;
    }

    /// <summary>Создать снимок из камеры.</summary>
    public static ViewportState From(Camera2D camera) =>
        new(camera.Zoom, camera.Pan, camera.Rotation, camera.Dpi, camera.ViewportSize);
}
