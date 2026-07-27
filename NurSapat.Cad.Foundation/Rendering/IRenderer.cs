namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Рендерер: берёт <see cref="RenderQueue"/> и рисует через <see cref="IRenderContext"/>.
/// </summary>
/// <remarks>
/// <b>Renderer никогда не обращается к Scene напрямую</b> — только к RenderQueue.
/// Это ключевое архитектурное ограничение: Scene → RenderQueueBuilder → RenderQueue → Renderer.
/// Конкретные реализации на Avalonia — Этап 3.
/// </remarks>
public interface IRenderer
{
    /// <summary>Отрисовать очередь на контексте.</summary>
    void Render(RenderQueue queue, IRenderContext context);

    /// <summary>Очистить поверхность.</summary>
    void Clear(IRenderContext context);
}
