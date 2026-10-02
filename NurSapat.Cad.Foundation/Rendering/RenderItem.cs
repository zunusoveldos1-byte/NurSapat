using NurSapat.Cad.Foundation.Layers;
using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Один элемент очереди отрисовки: неизменяемое описание того, что и где рисовать.
/// </summary>
/// <remarks>
/// Это «снимок» объекта, готовый для рендерера. Renderer не обращается к Scene —
/// он работает только со списком <see cref="RenderItem"/>.
/// </remarks>
public sealed record RenderItem
(
    Guid ObjectId,
    Guid LayerId,
    Bounds2D Bounds,
    RenderData? Data,
    int ZIndex
)
{
    public static RenderItem Empty { get; } = new(Guid.Empty, Guid.Empty, Bounds2D.Empty, null, 0);
}
