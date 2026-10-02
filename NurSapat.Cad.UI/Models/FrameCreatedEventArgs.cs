using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Models;

public sealed class FrameCreatedEventArgs : EventArgs
{
    public FrameCreatedEventArgs(GeometryElement element, bool replaceExisting)
    {
        Element = element;
        ReplaceExisting = replaceExisting;
    }

    public GeometryElement Element { get; }

    public bool ReplaceExisting { get; }
}
