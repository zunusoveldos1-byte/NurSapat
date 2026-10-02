using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Штапик — профиль по внутреннему периметру светового проёма.
/// Хранится в <see cref="GeometryElement"/> с Kind = Bead.
/// </summary>
public sealed class GlazingBeadModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ProfileId { get; set; }

    public Guid? MaterialId { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Offset { get; set; }

    public IReadOnlyList<ProfileEdge> Segments { get; set; } = [];

    public ProfileContourLoop? InnerContour { get; set; }

    public ProfileContourLoop? OuterContour { get; set; }

    public Guid OwnerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? HostSashId { get; set; }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public static GlazingBeadModel FromGeometryElement(GeometryElement element)
    {
        if (element.Kind != GeometryElementKind.Bead)
        {
            throw new ArgumentException("Элемент не является штапиком.", nameof(element));
        }

        return new GlazingBeadModel
        {
            Id = element.Id,
            ProfileId = element.ProfileSeriesId,
            MaterialId = element.MaterialId,
            Width = (double)element.WidthMm,
            Height = (double)element.HeightMm,
            Offset = element.BeadOffsetMm ?? 0,
            OwnerId = element.HostFrameNodeId ?? Guid.Empty,
            HostSashId = element.HostSashId,
            CenterX = (double)element.X,
            CenterY = (double)element.Y,
            WidthMm = (double)element.WidthMm,
            HeightMm = (double)element.HeightMm,
            CreatedAt = element.CreatedAt ?? DateTimeOffset.UtcNow
        };
    }

    public GeometryElement ToGeometryElement() => new()
    {
        Id = Id,
        Kind = GeometryElementKind.Bead,
        Label = "Штапик",
        SourceToolId = "bead",
        X = (decimal)CenterX,
        Y = (decimal)CenterY,
        WidthMm = (decimal)WidthMm,
        HeightMm = (decimal)HeightMm,
        MaterialId = MaterialId,
        ProfileSeriesId = ProfileId,
        HostFrameNodeId = OwnerId == Guid.Empty ? null : OwnerId,
        HostSashId = HostSashId,
        BeadOffsetMm = Offset,
        CreatedAt = CreatedAt
    };
}
