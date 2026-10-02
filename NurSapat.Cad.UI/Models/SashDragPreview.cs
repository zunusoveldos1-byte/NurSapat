using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Models;

public sealed class SashDragPreview
{
    public double CellOriginX { get; init; }

    public double CellOriginY { get; init; }

    public double CellWidth { get; init; }

    public double CellHeight { get; init; }

    public double OriginX { get; init; }

    public double OriginY { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    public SashHingeSide HingeSide { get; init; }
}
