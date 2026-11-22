using NurSapat.Cad.Foundation.Events;

namespace NurSapat.Cad.Foundation.Layers;

/// <summary>
/// Менеджер слоёв: высокоуровневые операции над <see cref="LayerCollection"/>
/// с публикацией событий через <see cref="ICadEventBus"/>.
/// </summary>
public interface ILayerManager
{
    /// <summary>Коллекция слоёв сцены.</summary>
    LayerCollection Layers { get; }

    /// <summary>Активный слой (куда по умолчанию добавляются новые объекты).</summary>
    LayerId ActiveLayer { get; set; }

    Layer AddLayer(string name);
    bool RemoveLayer(LayerId id);
    void RenameLayer(LayerId id, string newName);
    void SetVisible(LayerId id, bool visible);
    void SetLocked(LayerId id, bool locked);
    void SetColor(LayerId id, LayerColor color);
    void Reorder(LayerId id, int newOrder);
    Layer? Find(LayerId id);
}
