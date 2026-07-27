using NurSapat.Cad.Foundation.Events;
using NurSapat.Cad.Foundation.Layers;
using NurSapat.Cad.Foundation.Rendering;
using NurSapat.Cad.Foundation.Viewport;

namespace NurSapat.Cad.Foundation.Scene;

/// <summary>
/// Сцена CAD: единая точка хранения объектов редактора.
/// </summary>
/// <remarks>
/// Сцена владеет <see cref="Layers"/> и набором объектов (<see cref="Objects"/>).
/// Она — единственный источник правды для Scene Graph: UI не хранит геометрию,
/// а только отражает состояние сцены (через RenderQueue).
/// </remarks>
public interface IScene
{
    /// <summary>Слои сцены.</summary>
    LayerCollection Layers { get; }

    /// <summary>Все объекты сцены.</summary>
    SceneObjectCollection Objects { get; }

    /// <summary>Найти объект по идентификатору.</summary>
    SceneObject? Find(Guid objectId);

    /// <summary>Добавить объект в сцену (опционально с указанием слоя).</summary>
    void Add(SceneObject obj, LayerId? layer = null);

    /// <summary>Удалить объект из сцены.</summary>
    bool Remove(Guid objectId);

    /// <summary>Очистить сцену (объекты; системные слои остаются).</summary>
    void Clear();

    /// <summary>Построить очередь рендера для текущего состояния и вида.</summary>
    RenderQueue BuildRenderQueue(IViewport viewport);

    /// <summary>Границы, охватывающие все объекты сцены.</summary>
    Primitives.Bounds2D GetExtents();

    event EventHandler<ObjectCreatedEvent>? ObjectCreated;
    event EventHandler<ObjectDeletedEvent>? ObjectDeleted;
    event EventHandler<ObjectModifiedEvent>? ObjectModified;
}
