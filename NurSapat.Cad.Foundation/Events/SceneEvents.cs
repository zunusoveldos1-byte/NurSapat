namespace NurSapat.Cad.Foundation.Events;

/// <summary>Событие: объект создан и добавлен в сцену.</summary>
public sealed class ObjectCreatedEvent : CadEventBase
{
    public Guid ObjectId { get; }
    public Guid LayerId { get; }
    public string? ObjectName { get; }

    public ObjectCreatedEvent(Guid objectId, Guid layerId, string? objectName = null)
    {
        ObjectId = objectId;
        LayerId = layerId;
        ObjectName = objectName;
    }
}

/// <summary>Событие: объект удалён из сцены.</summary>
public sealed class ObjectDeletedEvent : CadEventBase
{
    public Guid ObjectId { get; }
    public ObjectDeletedEvent(Guid objectId) => ObjectId = objectId;
}

/// <summary>Событие: геометрия или свойства объекта изменились (позиция, размер, метаданные).</summary>
public sealed class ObjectModifiedEvent : CadEventBase
{
    public Guid ObjectId { get; }
    public string? PropertyName { get; }

    public ObjectModifiedEvent(Guid objectId, string? propertyName = null)
    {
        ObjectId = objectId;
        PropertyName = propertyName;
    }
}
