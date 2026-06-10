using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Viewport;

public sealed partial class CadViewport : ObservableObject
{
    public const double MinZoom = 0.1;
    public const double MaxZoom = 50.0;

    [ObservableProperty]
    private double _zoomFactor = 0.35;

    [ObservableProperty]
    private CadVector _panOffset = new(500, 350);

    public double Width { get; private set; } = 1000;
    public double Height { get; private set; } = 700;

    public void Resize(double width, double height)
    {
        var nextWidth = Math.Max(1, width);
        var nextHeight = Math.Max(1, height);
        if (Math.Abs(Width - nextWidth) < CadMath.Epsilon &&
            Math.Abs(Height - nextHeight) < CadMath.Epsilon)
        {
            return;
        }

        Width = nextWidth;
        Height = nextHeight;
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
    }

    public ScreenPoint WorldToScreen(CadPoint worldPt)
    {
        return new ScreenPoint(
            PanOffset.X + worldPt.X * ZoomFactor,
            PanOffset.Y - worldPt.Y * ZoomFactor);
    }

    public CadPoint ScreenToWorld(ScreenPoint screenPt)
    {
        return new CadPoint(
            (screenPt.X - PanOffset.X) / ZoomFactor,
            (PanOffset.Y - screenPt.Y) / ZoomFactor);
    }

    public void Pan(CadVector screenDelta)
    {
        PanOffset += screenDelta;
    }

    public void FitToWorld(double worldWidth, double worldHeight, double margin = 80)
    {
        FitToBounds(new CadRect(0, worldHeight, worldWidth, worldHeight), margin);
    }

    public void FitToBounds(CadRect bounds, double margin = 80)
    {
        var worldWidth = Math.Max(1, Math.Abs(bounds.Width));
        var worldHeight = Math.Max(1, Math.Abs(bounds.Height));
        var left = Math.Min(bounds.X, bounds.X + bounds.Width);
        var right = Math.Max(bounds.X, bounds.X + bounds.Width);
        var top = Math.Max(bounds.Y, bounds.Y - bounds.Height);
        var bottom = Math.Min(bounds.Y, bounds.Y - bounds.Height);
        var centerX = (left + right) / 2;
        var centerY = (top + bottom) / 2;
        var availableWidth = Math.Max(1, Width - margin * 2);
        var availableHeight = Math.Max(1, Height - margin * 2);
        var zoomX = availableWidth / worldWidth;
        var zoomY = availableHeight / worldHeight;
        ZoomFactor = CadMath.Clamp(Math.Min(zoomX, zoomY), MinZoom, MaxZoom);
        PanOffset = new CadVector(
            Width / 2 - centerX * ZoomFactor,
            Height / 2 + centerY * ZoomFactor);
    }

    public void ZoomAt(ScreenPoint cursor, double zoomMultiplier)
    {
        var worldBefore = ScreenToWorld(cursor);
        var newZoom = CadMath.Clamp(ZoomFactor * zoomMultiplier, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - ZoomFactor) < CadMath.Epsilon) return;

        ZoomFactor = newZoom;
        PanOffset = new CadVector(
            cursor.X - worldBefore.X * ZoomFactor,
            cursor.Y + worldBefore.Y * ZoomFactor);
    }

    public double ChooseGridStep()
    {
        var targetPixels = 72.0;
        var rawStep = targetPixels / ZoomFactor;
        var steps = new[] { 0.1d, 0.5d, 1d, 5d, 10d, 50d, 100d, 500d, 1000d, 5000d, 10000d, 50000d };
        return steps.FirstOrDefault(step => step >= rawStep, steps[^1]);
    }
}
