using NurSapat.Cad.Foundation.Events;
using NurSapat.Cad.Foundation.Layers;
using NurSapat.Cad.Foundation.Primitives;
using NurSapat.Cad.Foundation.Rendering;
using NurSapat.Cad.Foundation.Viewport;

namespace NurSapat.Cad.Foundation.Scene;

/// <summary>
/// Конкретная реализация <see cref="IScene"/> — корень Scene Graph.
/// </summary>
public sealed class Scene : IScene
{
    private readonly ICadEventBus _eventBus;

    public LayerCollection Layers { get; }
    public SceneObjectCollection Objects { get; }

    public Scene(ICadEventBus eventBus)
        : this(eventBus, new LayerCollection())
    {
    }

    public Scene(ICadEventBus eventBus, LayerCollection layers)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        Objects = new SceneObjectCollection();
    }

    public SceneObject? Find(Guid objectId) => Objects.Find(objectId);

    public void Add(SceneObject obj, LayerId? layer = null)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.LayerId = layer?.Id ?? obj.LayerId;
        if (obj.LayerId == Guid.Empty)
            obj.LayerId = LayerId.Default.Id;
        Objects.Add(obj);
        var created = new ObjectCreatedEvent(obj.ObjectId, obj.LayerId, obj.Name);
        _eventBus.Publish(created);
        ObjectCreated?.Invoke(this, created);
    }

    public bool Remove(Guid objectId)
    {
        if (!Objects.Remove(objectId)) return false;
        var deleted = new ObjectDeletedEvent(objectId);
        _eventBus.Publish(deleted);
        ObjectDeleted?.Invoke(this, deleted);
        return true;
    }

    public void Clear()
    {
        Objects.Clear();
    }

    public RenderQueue BuildRenderQuery(IViewport viewport)
    {
        var visibleArea = viewport.VisibleArea;
        var items = new List<RenderItem>(Objects.Count);

        foreach (var obj in Objects.Items)
        {
            if (!obj.Visible) continue;
            if (!obj.Bounds.Intersects(visibleArea) && !visibleArea.IsEmpty) continue;

            var layer = Layers.Find(new LayerId(obj.LayerId, string.Empty));
            if (layer is { Visible: false }) continue;

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

    /// <inheritdoc />
    RenderQueue IScene.BuildRenderQueue(IViewport viewport) => BuildRenderQuery(viewport);

    public Bounds2D GetExtents()
    {
        if (Objects.Count == 0) return Bounds2D.Empty;
        var bounds = Objects.Items[0].Bounds;
        for (var i = 1; i < Objects.Count; i++)
            bounds = bounds.Union(Objects.Items[i].Bounds);
        return bounds;
    }

    public event EventHandler<ObjectCreatedEvent>? ObjectCreated;
    public event EventHandler<ObjectDeletedEvent>? ObjectDeleted;

    // Reserved for future use: raised when scene object properties are updated in-place.
#pragma warning disable CS0067
    public event EventHandler<ObjectModifiedEvent>? ObjectModified;
#pragma warning restore CS0067
}
