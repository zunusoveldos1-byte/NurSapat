using System.Collections.ObjectModel;

namespace NurSapat.Cad.Foundation.Scene;

/// <summary>
/// Набор объектов сцены с O(1)-доступом по <see cref="Guid"/>.
/// </summary>
public sealed class SceneObjectCollection
{
    private readonly ObservableCollection<SceneObject> _items = new();
    private readonly Dictionary<Guid, SceneObject> _byId = new();

    public IReadOnlyList<SceneObject> Items => _items;
    public int Count => _items.Count;

    public SceneObject this[int index] => _items[index];

    public event EventHandler<SceneObject>? Added;
    public event EventHandler<SceneObject>? Removed;

    /// <summary>Найти объект по идентификатору.</summary>
    public SceneObject? Find(Guid objectId) =>
        _byId.TryGetValue(objectId, out var obj) ? obj : null;

    /// <summary>Содержится ли объект с данным идентификатором.</summary>
    public bool Contains(Guid objectId) => _byId.ContainsKey(objectId);

    /// <summary>Добавить объект. Дубликаты по ObjectId игнорируются.</summary>
    public void Add(SceneObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (_byId.ContainsKey(obj.ObjectId)) return;
        _byId[obj.ObjectId] = obj;
        _items.Add(obj);
        Added?.Invoke(this, obj);
    }

    /// <summary>Удалить объект по идентификатору.</summary>
    public bool Remove(Guid objectId)
    {
        if (!_byId.TryGetValue(objectId, out var obj)) return false;
        _byId.Remove(objectId);
        _items.Remove(obj);
        Removed?.Invoke(this, obj);
        return true;
    }

    /// <summary>Удалить все объекты.</summary>
    public void Clear()
    {
        var removed = _items.ToArray();
        _items.Clear();
        _byId.Clear();
        foreach (var obj in removed)
            Removed?.Invoke(this, obj);
    }

    /// <summary>Перечислить объекты, удовлетворяющие предикату.</summary>
    public IEnumerable<SceneObject> Where(Func<SceneObject, bool> predicate)
    {
        foreach (var item in _items)
            if (predicate(item))
                yield return item;
    }
}
