namespace NurSapat.Core.Models.Orders;

/// <summary>
/// Габаритная фиктивная коробка — невидимый контейнер для сложных составных конструкций
/// (балконные блоки, П- и Т-образные фасады). Не участвует в списании материалов.
/// </summary>
public sealed class FictitiousFrame
{
    public const bool IsFictitious = true;

    public Guid Id { get; set; } = Guid.NewGuid();

    public double OriginX { get; set; }

    public double OriginY { get; set; }

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    /// <summary>Идентификаторы дочерних элементов внутри габаритной рамки.</summary>
    public List<Guid> ChildElementIds { get; } = [];

    public static bool IsFictitiousKind(GeometryElementKind kind) =>
        kind is GeometryElementKind.FictitiousFrame
            or GeometryElementKind.DummyFrame
            or GeometryElementKind.RoundDummyFrame
            or GeometryElementKind.TriangleDummyFrame
            or GeometryElementKind.ArchDummyFrame
            or GeometryElementKind.DummyMullion;

    public static bool IsFictitiousElement(GeometryElement element) => IsFictitiousKind(element.Kind);

    public static FictitiousFrame FromElement(GeometryElement element)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        return new FictitiousFrame
        {
            Id = element.Id,
            OriginX = (double)element.X - width / 2.0,
            OriginY = (double)element.Y - height / 2.0,
            WidthMm = width,
            HeightMm = height
        };
    }

    public GeometryElement ToElement(string? label = null) =>
        new()
        {
            Id = Id,
            Kind = GeometryElementKind.FictitiousFrame,
            Label = label ?? "Коробка (фиктивная)",
            X = (decimal)(OriginX + WidthMm / 2.0),
            Y = (decimal)(OriginY + HeightMm / 2.0),
            WidthMm = (decimal)WidthMm,
            HeightMm = (decimal)HeightMm
        };

    public static IEnumerable<GeometryElement> EnumerateBillableElements(IEnumerable<GeometryElement> elements) =>
        elements.Where(element => element.IsBillable);
}
