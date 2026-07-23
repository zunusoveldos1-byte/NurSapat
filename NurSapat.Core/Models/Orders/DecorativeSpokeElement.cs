namespace NurSapat.Core.Models.Orders;

/// <summary>
/// Декоративная раскладка (шпросы) на стеклопакете.
/// Хранится в <see cref="GeometryElement"/> с <see cref="GeometryElementKind.DecorativeSpoke"/>.
/// </summary>
public sealed class DecorativeSpokeElement
{
    public static readonly double[] StandardThicknessesMm = [8, 18, 26];

    public static class Colors
    {
        public const string Gold = "gold";
        public const string White = "white";
        public const string Brown = "brown";
    }

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParentGlazingId { get; set; }

    public DecorativeSpokeType SpokeType { get; set; } = DecorativeSpokeType.StandardGrid;

    public double Thickness { get; set; } = 18;

    public string Color { get; set; } = Colors.White;

    public int RowsCount { get; set; } = 2;

    public int ColumnsCount { get; set; } = 2;

    public int RayCount { get; set; } = 4;

    public bool IsArched { get; set; }

    public double ArcCenterX { get; set; }

    public double ArcCenterY { get; set; }

    public double ArcRadius { get; set; }

    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public Guid? MaterialId { get; set; }

    public Guid? ProfileSeriesId { get; set; }

    public static DecorativeSpokeElement FromGeometryElement(GeometryElement element)
    {
        if (element.Kind != GeometryElementKind.DecorativeSpoke)
        {
            throw new ArgumentException("Элемент не является декоративным шпросом.", nameof(element));
        }

        return new DecorativeSpokeElement
        {
            Id = element.Id,
            ParentGlazingId = element.ParentGlazingId ?? Guid.Empty,
            SpokeType = element.SpokeType ?? DecorativeSpokeType.StandardGrid,
            Thickness = element.SpokeThicknessMm ?? 18,
            Color = element.SpokeColor ?? Colors.White,
            RowsCount = element.SpokeRowsCount ?? 2,
            ColumnsCount = element.SpokeColumnsCount ?? 2,
            RayCount = element.SpokeRayCount ?? 4,
            IsArched = element.SpokeIsArched,
            ArcCenterX = (double)(element.SpokeArcCenterX ?? element.X),
            ArcCenterY = (double)(element.SpokeArcCenterY ?? element.Y),
            ArcRadius = element.SpokeArcRadius ?? 0,
            CenterX = (double)element.X,
            CenterY = (double)element.Y,
            WidthMm = (double)element.WidthMm,
            HeightMm = (double)element.HeightMm,
            MaterialId = element.MaterialId,
            ProfileSeriesId = element.ProfileSeriesId
        };
    }

    public GeometryElement ToGeometryElement() => new()
    {
        Id = Id,
        Kind = GeometryElementKind.DecorativeSpoke,
        Label = "Декоративный шпрос",
        X = (decimal)CenterX,
        Y = (decimal)CenterY,
        WidthMm = (decimal)WidthMm,
        HeightMm = (decimal)HeightMm,
        ParentGlazingId = ParentGlazingId,
        SpokeType = SpokeType,
        SpokeThicknessMm = Thickness,
        SpokeColor = Color,
        SpokeRowsCount = RowsCount,
        SpokeColumnsCount = ColumnsCount,
        SpokeRayCount = RayCount,
        SpokeIsArched = IsArched,
        SpokeArcCenterX = (decimal)ArcCenterX,
        SpokeArcCenterY = (decimal)ArcCenterY,
        SpokeArcRadius = ArcRadius,
        MaterialId = MaterialId,
        ProfileSeriesId = ProfileSeriesId,
        SourceToolId = "decorative-spoke"
    };

    public static bool TryFromGeometryElement(GeometryElement? element, out DecorativeSpokeElement spoke)
    {
        spoke = null!;
        if (element is null || element.Kind != GeometryElementKind.DecorativeSpoke)
        {
            return false;
        }

        spoke = FromGeometryElement(element);
        return true;
    }
}
