using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Viewport;

/// <summary>
/// Вид CAD: преобразования между мировыми и экранными координатами, zoom, pan.
/// </summary>
/// <remarks>
/// Реализация по умолчанию — <see cref="Camera2D"/>. Существующий
/// <c>NurSapat.Cad.Core.CadViewport</c> будет реализовывать этот интерфейс
/// (мост наследованием) на этапе интеграции редактора.
/// </remarks>
public interface IViewport
{
    /// <summary>Масштаб (пикселей на единицу мира или безразмерный коэффициент — зависит от реализации).</summary>
    double Zoom { get; set; }

    /// <summary>Сдвиг начала мира в экранных координатах.</summary>
    Point2D Pan { get; set; }

    /// <summary>Центр видимой области в мировых координатах.</summary>
    Point2D Center { get; }

    /// <summary>Видимая прямоугольная область в мировых координатах.</summary>
    Bounds2D VisibleArea { get; }

    /// <summary>Поворот вида в радианах (подготовка; в 2D обычно 0).</summary>
    double Rotation { get; set; }

    /// <summary>DPI поверхности отрисовки.</summary>
    double Dpi { get; set; }

    /// <summary>Размер viewport в экранных пикселях.</summary>
    Primitives.Size ViewportSize { get; set; }

    /// <summary>Преобразовать мировую точку в экранные координаты.</summary>
    Point2D WorldToScreen(Point2D world);

    /// <summary>Преобразовать экранную точку в мировые координаты.</summary>
    Point2D ScreenToWorld(Point2D screen);

    /// <summary>Уместить в вид заданные мировые границы.</summary>
    void ZoomToFit(Bounds2D bounds);

    /// <summary>Уместить в вид текущее выделение (по границам объектов).</summary>
    void ZoomToSelection(IEnumerable<Guid> objectIds);
}
