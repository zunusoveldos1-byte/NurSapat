using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Events;

/// <summary>Событие: параметры вида (zoom/pan/центр) изменились.</summary>
public sealed class ViewportChangedEvent : CadEventBase
{
    public double Zoom { get; }
    public Point2D Center { get; }

    public ViewportChangedEvent(double zoom, Point2D center)
    {
        Zoom = zoom;
        Center = center;
    }
}
