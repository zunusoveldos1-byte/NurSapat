using NurSapat.Cad.Foundation.Events;

namespace NurSapat.Cad.Foundation.Selection;

/// <summary>
/// Аргументы события изменения выделения.
/// </summary>
public sealed class SelectionChangedEventArgs : EventArgs
{
    public IReadOnlyList<Guid> Selected { get; }
    public IReadOnlyList<Guid> Deselected { get; }

    public SelectionChangedEventArgs(IReadOnlyList<Guid> selected, IReadOnlyList<Guid> deselected)
    {
        Selected = selected ?? Array.Empty<Guid>();
        Deselected = deselected ?? Array.Empty<Guid>();
    }
}

/// <summary>
/// Менеджер выделения: управляет множественным выбором по ObjectId.
/// Никакой логики инструментов — только выбор/отмена/очистка.
/// </summary>
public interface ISelectionManager
{
    /// <summary>Текущий набор выделенных ObjectId.</summary>
    IReadOnlyList<Guid> SelectedIds { get; }

    /// <summary>Число выделенных объектов.</summary>
    int Count { get; }

    /// <summary>Хотя бы один объект выделен.</summary>
    bool HasSelection { get; }

    /// <summary>Поднимается при любом изменении выделения.</summary>
    event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>Выделить объект.</summary>
    void Select(Guid objectId);

    /// <summary>Выделить группу объектов.</summary>
    void Select(IEnumerable<Guid> objectIds);

    /// <summary>Снять выделение с объекта.</summary>
    void Deselect(Guid objectId);

    /// <summary>Переключить выделение объекта (toggle).</summary>
    void Toggle(Guid objectId);

    /// <summary>Снять выделение со всех объектов.</summary>
    void Clear();

    /// <summary>Выделить все объекты сцены.</summary>
    void SelectAll();

    /// <summary>Содержится ли объект в текущем выделении.</summary>
    bool IsSelected(Guid objectId);
}
