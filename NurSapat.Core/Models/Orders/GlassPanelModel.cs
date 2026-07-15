namespace NurSapat.Core.Models.Orders;

/// <summary>
/// Стеклопакет внутри штапика.
/// Хранится в <see cref="GeometryElement"/> с <see cref="GeometryElementKind.Glazing"/>.
/// </summary>
public sealed class GlassPanelModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? MaterialId { get; set; }

    public Guid? ParentBeadId { get; set; }

    public Guid? HostSashId { get; set; }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public static GlassPanelModel FromGeometryElement(GeometryElement element)
    {
        if (element.Kind != GeometryElementKind.Glazing)
        {
            throw new ArgumentException("Элемент не является остеклением.", nameof(element));
        }

        return new GlassPanelModel
        {
            Id = element.Id,
            MaterialId = element.MaterialId,
            ParentBeadId = element.ParentBeadId,
            HostSashId = element.HostSashId,
            CenterX = (double)element.X,
            CenterY = (double)element.Y,
            WidthMm = (double)element.WidthMm,
            HeightMm = (double)element.HeightMm
        };
    }

    public GeometryElement ToGeometryElement() => new()
    {
        Id = Id,
        Kind = GeometryElementKind.Glazing,
        Label = "Остекление",
        SourceToolId = "bead-glass",
        X = (decimal)CenterX,
        Y = (decimal)CenterY,
        WidthMm = (decimal)WidthMm,
        HeightMm = (decimal)HeightMm,
        MaterialId = MaterialId,
        ParentBeadId = ParentBeadId,
        HostSashId = HostSashId,
        HasBead = ParentBeadId is not null
    };
}
