namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Абстрактный базовый класс данных отрисовки объекта.
/// </summary>
/// <remarks>
/// Конкретные подклассы (LineRenderData, PolylineRenderData, TextRenderData, ...)
/// появятся на Этапе 3 (Rendering Engine). Рендерер интерпретирует их через
/// полиморфизм или сопоставление типов. Ядро намеренно не знает, как рисовать.
/// </remarks>
public abstract class RenderData
{
    /// <summary>Дискриминатор типа данных (для рендерера и сериализации).</summary>
    public abstract string Kind { get; }
}
