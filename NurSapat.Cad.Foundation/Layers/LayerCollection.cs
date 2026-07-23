using System.Collections.ObjectModel;

namespace NurSapat.Cad.Foundation.Layers;

/// <summary>
/// Упорядоченный набор слоёв сцены с быстрым доступом по <see cref="LayerId"/>.
/// </summary>
/// <remarks>
/// Содержит стандартные системные слои (Default, Dimensions, Guides, Selection, Grid, Preview)
/// и позволяет добавлять пользовательские. Порядок определяется <see cref="Layer.Order"/>
/// (визуально «снизу вверх» — от большего Z к меньшему при отрисовке).
/// </remarks>
public sealed class LayerCollection
{
    private readonly ObservableCollection<Layer> _layers = new();
    private readonly Dictionary<Guid, Layer> _byId = new();

    /// <summary>Слои в порядке отображения (readonly-обёртка).</summary>
    public IReadOnlyList<Layer> Layers => _layers;

    /// <summary>Число слоёв.</summary>
    public int Count => _layers.Count;

    public LayerCollection(bool createDefaults = true)
    {
        if (createDefaults)
        {
            AddDefaultLayers();
        }
    }

    /// <summary>Найти слой по идентификатору. Возвращает null, если не найден.</summary>
    public Layer? Find(LayerId id) => _byId.TryGetValue(id.Id, out var layer) ? layer : null;

    /// <summary>Получить слой по идентификатору. Бросает, если не найден.</summary>
    public Layer Get(LayerId id) =>
        _byId.TryGetValue(id.Id, out var layer)
            ? layer
            : throw new KeyNotFoundException($"Слой «{id.Name}» ({id.Id}) не найден.");

    /// <summary>Добавить слой. Если уже существует — возвращает существующий.</summary>
    public Layer Add(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (_byId.ContainsKey(layer.Id.Id))
            return _byId[layer.Id.Id];
        _byId[layer.Id.Id] = layer;
        _layers.Add(layer);
        return layer;
    }

    /// <summary>Создать и добавить пользовательский слой.</summary>
    public Layer Add(string name) => Add(new Layer(LayerId.New(name), name));

    /// <summary>Удалить слой. Системные удалить нельзя.</summary>
    public bool Remove(LayerId id)
    {
        if (!_byId.TryGetValue(id.Id, out var layer)) return false;
        if (layer.IsSystem)
            throw new InvalidOperationException($"Системный слой «{id.Name}» нельзя удалить.");
        _byId.Remove(id.Id);
        _layers.Remove(layer);
        return true;
    }

    /// <summary>Переименовать слой.</summary>
    public void Rename(LayerId id, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Имя слоя не может быть пустым.", nameof(newName));
        Get(id).Name = newName;
    }

    /// <summary>Задать видимость слоя.</summary>
    public void SetVisible(LayerId id, bool visible) => Get(id).Visible = visible;

    /// <summary>Задать блокировку слоя.</summary>
    public void SetLocked(LayerId id, bool locked) => Get(id).Locked = locked;

    /// <summary>Задать цвет слоя.</summary>
    public void SetColor(LayerId id, LayerColor color) => Get(id).Color = color;

    /// <summary>Изменить порядок слоя относительно соседа.</summary>
    public void Reorder(LayerId id, int newOrder) => Get(id).Order = newOrder;

    /// <summary>Очистить все пользовательские слои (системные остаются).</summary>
    public void ClearUserLayers()
    {
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            if (!_layers[i].IsSystem)
            {
                _byId.Remove(_layers[i].Id.Id);
                _layers.RemoveAt(i);
            }
        }
    }

    private void AddDefaultLayers()
    {
        // Порядок по умолчанию: Grid (самый нижний) → Guides → Default → Dimensions → Preview → Selection (верхний)
        Add(new Layer(LayerId.Grid) { Order = 100, Visible = true });
        Add(new Layer(LayerId.Guides) { Order = 200, Visible = true });
        Add(new Layer(LayerId.Default) { Order = 300, Visible = true });
        Add(new Layer(LayerId.Dimensions) { Order = 400, Visible = true });
        Add(new Layer(LayerId.Preview) { Order = 500, Visible = true });
        Add(new Layer(LayerId.Selection) { Order = 600, Visible = true });
    }
}
