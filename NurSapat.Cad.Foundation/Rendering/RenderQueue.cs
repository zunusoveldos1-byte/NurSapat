using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Неизменяемая очередь отрисовки — снимок видимых элементов сцены,
/// отсортированный в порядке отрисовки (по Z-индексу/слою).
/// </summary>
/// <remarks>
/// Строится <see cref="IRenderQueueBuilder"/>-ом из <c>Scene</c> и передаётся
/// рендереру. Это ключевая граница: <b>renderer никогда не работает со Scene напрямую</b>.
/// </remarks>
public sealed class RenderQueue
{
    public static RenderQueue Empty { get; } = new(Array.Empty<RenderItem>());

    /// <summary>Элементы в порядке отрисовки (нижние первыми).</summary>
    public IReadOnlyList<RenderItem> Items { get; }

    /// <summary>Границы всех элементов очереди.</summary>
    public Bounds2D Extents { get; }

    public bool IsEmpty => Items.Count == 0;

    public RenderQueue(IReadOnlyList<RenderItem> items)
    {
        Items = items ?? Array.Empty<RenderItem>();
        Extents = ComputeExtents(Items);
    }

    private static Bounds2D ComputeExtents(IReadOnlyList<RenderItem> items)
    {
        if (items.Count == 0) return Bounds2D.Empty;
        var bounds = items[0].Bounds;
        for (var i = 1; i < items.Count; i++)
            bounds = bounds.Union(items[i].Bounds);
        return bounds;
    }
}
