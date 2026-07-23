using NurSapat.Cad.Foundation.Scene;
using NurSapat.Cad.Foundation.Viewport;

namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Строитель <see cref="RenderQueue"/>: берёт <see cref="Scene"/> и <see cref="IViewport"/>
/// и формирует отсортированную очередь видимых элементов для рендерера.
/// </summary>
/// <remarks>
/// Разделяет Scene от Renderer: Renderer не знает о Scene, а Scene не знает о Renderer.
/// Это bridge-компонент.
/// </remarks>
public interface IRenderQueueBuilder
{
    /// <summary>Построить очередь рендера для текущего состояния сцены и вида.</summary>
    RenderQueue Build(IScene scene, IViewport viewport);
}
