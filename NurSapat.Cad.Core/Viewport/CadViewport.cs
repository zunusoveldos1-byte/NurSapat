using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Viewport;

public sealed class CadViewport
{
    public const double MinZoom = 0.1;
    public const double MaxZoom = 50.0;

    public double ZoomFactor { get; private set; } = 0.35;

    public CadVector PanOffset { get; private set; } = new(500, 350);

    public double Width { get; private set; } = 1000;

    public double Height { get; private set; } = 700;

    public void Resize(double width, double height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
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
        var zoomX = (Width - margin * 2) / Math.Max(1, worldWidth);
        var zoomY = (Height - margin * 2) / Math.Max(1, worldHeight);
        ZoomFactor = CadMath.Clamp(Math.Min(zoomX, zoomY), MinZoom, MaxZoom);
        PanOffset = new CadVector(
            (Width - worldWidth * ZoomFactor) / 2,
            Height - (Height - worldHeight * ZoomFactor) / 2);
    }

    public void ZoomAt(ScreenPoint cursor, double zoomMultiplier)
    {
        var worldBefore = ScreenToWorld(cursor);
        var newZoom = CadMath.Clamp(ZoomFactor * zoomMultiplier, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - ZoomFactor) < CadMath.Epsilon)
        {
            return;
        }

        ZoomFactor = newZoom;
        PanOffset = new CadVector(
            cursor.X - worldBefore.X * ZoomFactor,
            cursor.Y + worldBefore.Y * ZoomFactor);
    }

    public double ChooseGridStep()
    {
        var targetPixels = 72.0;
        var rawStep = targetPixels / ZoomFactor;
        var steps = new[] { 1d, 5d, 10d, 50d, 100d, 500d, 1000d, 5000d };
        return steps.First(step => step >= rawStep);
    }
}
