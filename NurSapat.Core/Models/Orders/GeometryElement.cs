namespace NurSapat.Core.Models.Orders;

public enum GeometryElementKind
{
    DummyFrame,
    DummyMullion,
    Frame,
    EdgeMullion,
    RoundDummyFrame,
    TransitionFrame,
    Sash,
    Hardware,
    Handle,
    Mullion,
    Corrugation,
    Glazing,
    Bead,
    MosquitoNet,
    SlidingSash,
    Installation,
    Accessory
}

public enum SashOpeningType
{
    Fixed,
    Turn,
    TiltTurn,
    SlideLeft,
    SlideRight
}

public sealed class GeometryElement
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public GeometryElementKind Kind { get; set; }

    public string Label { get; set; } = "";

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public SashOpeningType? OpeningType { get; set; }

    public Guid? MaterialId { get; set; }

    /// <summary>Id инструмента панели (mullion-v, slide-left и т.д.) для варианта отрисовки.</summary>
    public string? SourceToolId { get; set; }

    public decimal? EndX { get; set; }

    public decimal? EndY { get; set; }
}
