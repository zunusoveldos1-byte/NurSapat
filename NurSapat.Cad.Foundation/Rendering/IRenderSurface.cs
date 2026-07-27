namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Целевая поверхность отрисовки (фреймбуфер, холст, текстура).
/// </summary>
/// <remarks>
/// Абстракция — без привязки к UI-фреймворку. Реализации создаются
/// на Этапе 3 (Avalonia), Этапе 6 (Skia/PDF export) и т.д.
/// </remarks>
public interface IRenderSurface
{
    /// <summary>Получить контекст отрисовки для текущего кадра.</summary>
    IRenderContext BeginDraw();

    /// <summary>Завершить кадр и представить результат.</summary>
    void EndDraw();
}
