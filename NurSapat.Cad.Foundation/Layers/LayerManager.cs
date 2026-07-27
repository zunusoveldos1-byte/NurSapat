using NurSapat.Cad.Foundation.Events;

namespace NurSapat.Cad.Foundation.Layers;

/// <summary>
/// Реализация <see cref="ILayerManager"/>: операции над слоями с публикацией
/// <see cref="LayerChangedEvent"/> в шину событий.
/// </summary>
public sealed class LayerManager : ILayerManager
{
    private readonly ICadEventBus _eventBus;

    public LayerCollection Layers { get; }

    public LayerId ActiveLayer { get; set; } = LayerId.Default;

    public LayerManager(ICadEventBus eventBus)
        : this(eventBus, new LayerCollection())
    {
    }

    public LayerManager(ICadEventBus eventBus, LayerCollection layers)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
    }

    public Layer AddLayer(string name)
    {
        var layer = Layers.Add(name);
        _eventBus.Publish(new LayerChangedEvent(layer.Id.Id, LayerChangeKind.Added, layer.Name));
        return layer;
    }

    public bool RemoveLayer(LayerId id)
    {
        var name = Layers.Find(id)?.Name;
        if (!Layers.Remove(id)) return false;
        _eventBus.Publish(new LayerChangedEvent(id.Id, LayerChangeKind.Removed, name));
        if (ActiveLayer == id) ActiveLayer = LayerId.Default;
        return true;
    }

    public void RenameLayer(LayerId id, string newName)
    {
        Layers.Rename(id, newName);
        _eventBus.Publish(new LayerChangedEvent(id.Id, LayerChangeKind.Renamed, newName));
    }

    public void SetVisible(LayerId id, bool visible)
    {
        Layers.SetVisible(id, visible);
        _eventBus.Publish(new LayerChangedEvent(id.Id, LayerChangeKind.VisibilityChanged));
    }

    public void SetLocked(LayerId id, bool locked)
    {
        Layers.SetLocked(id, locked);
        _eventBus.Publish(new LayerChangedEvent(id.Id, LayerChangeKind.LockChanged));
    }

    public void SetColor(LayerId id, LayerColor color)
    {
        Layers.SetColor(id, color);
        _eventBus.Publish(new LayerChangedEvent(id.Id, LayerChangeKind.ColorChanged));
    }

    public void Reorder(LayerId id, int newOrder)
    {
        Layers.Reorder(id, newOrder);
        _eventBus.Publish(new LayerChangedEvent(id.Id, LayerChangeKind.Reordered));
    }

    public Layer? Find(LayerId id) => Layers.Find(id);
}
