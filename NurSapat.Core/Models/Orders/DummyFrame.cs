namespace NurSapat.Core.Models.Orders;

/// <summary>
/// Коробка фиктивная — плоский контейнер для разметки проёмов и вложенных конструкций.
/// Не участвует в расчёте профилей, стоимости и производственных данных.
/// </summary>
public sealed class DummyFrame
{
    public const bool IsDummy = true;

    public Guid Id { get; set; } = Guid.NewGuid();

    public double OriginX { get; set; }

    public double OriginY { get; set; }

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    /// <summary>Идентификаторы вложенных элементов (импосты, рамы, переходные профили).</summary>
    public List<Guid> ChildElementIds { get; } = [];

    public static bool IsDummyKind(GeometryElementKind kind) =>
        kind is GeometryElementKind.DummyFrame
            or GeometryElementKind.RoundDummyFrame
            or GeometryElementKind.TriangleDummyFrame
            or GeometryElementKind.ArchDummyFrame;

    public static bool IsDummyElement(GeometryElement element) => IsDummyKind(element.Kind);

    public static DummyFrame FromElement(GeometryElement element)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        return new DummyFrame
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
            Kind = GeometryElementKind.DummyFrame,
            Label = label ?? "Коробка (фиктивная)",
            X = (decimal)(OriginX + WidthMm / 2.0),
            Y = (decimal)(OriginY + HeightMm / 2.0),
            WidthMm = (decimal)WidthMm,
            HeightMm = (decimal)HeightMm
        };
}
