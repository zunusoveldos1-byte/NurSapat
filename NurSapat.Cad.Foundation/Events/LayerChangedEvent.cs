namespace NurSapat.Cad.Foundation.Events;

/// <summary>Тип изменения слоя.</summary>
public enum LayerChangeKind
{
    Added,
    Removed,
    Renamed,
    VisibilityChanged,
    LockChanged,
    ColorChanged,
    Reordered
}

/// <summary>Событие: структура или свойства слоёв изменились.</summary>
public sealed class LayerChangedEvent : CadEventBase
{
    public Guid LayerId { get; }
    public LayerChangeKind Kind { get; }
    public string? LayerName { get; }

    public LayerChangedEvent(Guid layerId, LayerChangeKind kind, string? layerName = null)
    {
        LayerId = layerId;
        Kind = kind;
        LayerName = layerName;
    }
}
