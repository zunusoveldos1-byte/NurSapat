using NurSapat.Cad.Foundation.Layers;
using NurSapat.Cad.Foundation.Scene;
using NurSapat.Cad.Foundation.Viewport;

namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Реализация <see cref="IRenderQueueBuilder"/>: строит RenderQueue из Scene и IViewport.
/// </summary>
public sealed class RenderQueueBuilder : IRenderQueueBuilder
{
    public RenderQueue Build(IScene scene, IViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(viewport);

        var visibleArea = viewport.VisibleArea;
        var items = new List<RenderItem>(scene.Objects.Count);

        foreach (var obj in scene.Objects.Items)
        {
            if (!obj.Visible) continue;

            // Фильтрация по видимой области (если viewport непустой и объект имеет границы)
            if (!visibleArea.IsEmpty && !obj.Bounds.IsEmpty && !obj.Bounds.Intersects(visibleArea))
                continue;

            // Проверяем видимость слоя
            var layer = scene.Layers.Find(new LayerId(obj.LayerId, string.Empty));
            if (layer is not null && !layer.Visible) continue;

            items.Add(new RenderItem(
                ObjectId: obj.ObjectId,
                LayerId: obj.LayerId,
                Bounds: obj.Bounds,
                Data: obj.GetRenderData(),
                ZIndex: layer?.Order ?? 0));
        }

        // Сортировка по Z-индексу: нижние (меньший Z) рисуются первыми.
        items.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
        return new RenderQueue(items);
    }
}
