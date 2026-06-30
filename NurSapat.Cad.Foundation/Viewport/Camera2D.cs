using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Viewport;

/// <summary>
/// Двумерная камера: инкапсулирует математику преобразований world ↔ screen,
/// zoom, pan, поворот. Базовая архитектура для будущей <c>Camera3D</c>.
/// </summary>
public sealed partial class Camera2D : ObservableObject, IViewport
{
    public const double MinZoom = 0.0001;
    public const double MaxZoom = 1_000_000.0;

    [ObservableProperty]
    private double _zoom = 1.0;

    /// <summary>Сдвиг начала мира в экранных координатах (px).</summary>
    [ObservableProperty]
    private Point2D _pan = Point2D.Zero;

    [ObservableProperty]
    private double _rotation;

    [ObservableProperty]
    private double _dpi = 96.0;

    [ObservableProperty]
    private Size _viewportSize = new(1000, 700);

    public Camera2D()
    {
    }

    public Camera2D(Size viewportSize) => _viewportSize = viewportSize;

    /// <summary>Центр видимой области в мировых координатах.</summary>
    public Point2D Center => ScreenToWorld(new Point2D(ViewportSize.Width * 0.5, ViewportSize.Height * 0.5));

    /// <summary>Видимая прямоугольная область в мировых координатах.</summary>
    public Bounds2D VisibleArea
    {
        get
        {
            var min = ScreenToWorld(Point2D.Zero);
            var max = ScreenToWorld(new Point2D(ViewportSize.Width, ViewportSize.Height));
            return Bounds2D.FromPoints(min, max);
        }
    }

    /// <inheritdoc />
    public Point2D WorldToScreen(Point2D world)
    {
        // Применяем поворот вокруг центра экрана, затем масштаб + pan.
        if (Rotation != 0)
        {
            var cos = Math.Cos(Rotation);
            var sin = Math.Sin(Rotation);
            var cx = ViewportSize.Width * 0.5;
            var cy = ViewportSize.Height * 0.5;
            var dx = world.X - 0; // центр поворота — мировое начало (упрощение для 2D)
            var dy = world.Y - 0;
            world = new Point2D(cx + (dx * cos - dy * sin), cy + (dx * sin + dy * cos));
        }
        return new Point2D(Pan.X + world.X * Zoom, Pan.Y - world.Y * Zoom);
    }

    /// <inheritdoc />
    public Point2D ScreenToWorld(Point2D screen)
    {
        var world = new Point2D((screen.X - Pan.X) / Zoom, (Pan.Y - screen.Y) / Zoom);
        if (Rotation == 0) return world;
        var cos = Math.Cos(-Rotation);
        var sin = Math.Sin(-Rotation);
        var cx = ViewportSize.Width * 0.5;
        var cy = ViewportSize.Height * 0.5;
        var dx = world.X - cx;
        var dy = world.Y - cy;
        return new Point2D(dx * cos - dy * sin, dx * sin + dy * cos);
    }

    /// <inheritdoc />
    public void ZoomToFit(Bounds2D bounds)
    {
        if (bounds.IsEmpty)
        {
            Zoom = 1.0;
            Pan = Point2D.Zero;
            return;
        }
        var margin = 0.1; // 10% отступа
        var availableWidth = ViewportSize.Width * (1 - margin);
        var availableHeight = ViewportSize.Height * (1 - margin);
        var zoomX = availableWidth / Math.Max(bounds.Width, double.Epsilon);
        var zoomY = availableHeight / Math.Max(bounds.Height, double.Epsilon);
        Zoom = ClampZoom(Math.Min(zoomX, zoomY));
        Pan = new Point2D(
            ViewportSize.Width * 0.5 - bounds.Center.X * Zoom,
            ViewportSize.Height * 0.5 + bounds.Center.Y * Zoom);
    }

    /// <inheritdoc />
    public void ZoomToSelection(IEnumerable<Guid> objectIds)
    {
        // По границам объектов — реализация требует IScene; оставляем заглушкой,
        // сервисный слой (ViewportService) построит bounds и вызовет ZoomToFit.
        if (objectIds is null) return;
    }

    /// <summary>Изменить масштаб вокруг точки курсора (сохраняя мир под курсором).</summary>
    public void ZoomAt(Point2D screenAnchor, double zoomFactor)
    {
        var worldBefore = ScreenToWorld(screenAnchor);
        Zoom = ClampZoom(Zoom * zoomFactor);
        Pan = new Point2D(
            screenAnchor.X - worldBefore.X * Zoom,
            screenAnchor.Y + worldBefore.Y * Zoom);
    }

    /// <summary>Сдвинуть камеру на screenDelta (px).</summary>
    public void PanBy(Vector2D screenDelta) => Pan = new Point2D(Pan.X + screenDelta.X, Pan.Y + screenDelta.Y);

    private static double ClampZoom(double value) => Math.Clamp(value, MinZoom, MaxZoom);
}
