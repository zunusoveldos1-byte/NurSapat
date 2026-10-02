using NurSapat.Cad.Foundation.Commands;
using NurSapat.Cad.Foundation.Events;
using NurSapat.Cad.Foundation.Layers;
using NurSapat.Cad.Foundation.Primitives;
using NurSapat.Cad.Foundation.Scene;
using NurSapat.Cad.Foundation.Selection;
using NurSapat.Cad.Foundation.Snapping;
using NurSapat.Cad.Foundation.Viewport;
using SceneImpl = NurSapat.Cad.Foundation.Scene.Scene;

namespace NurSapat.Cad.Foundation.Workspace;

/// <summary>
/// Рабочее пространство: один открытый документ со своей сценой, viewport, историей,
/// выделением, слоями и шиной событий. Используется <see cref="WorkspaceManager"/>
/// для поддержки нескольких одновременно открытых документов.
/// </summary>
public sealed class Workspace : IWorkspace
{
    private bool _disposed;

    public WorkspaceId Id { get; }
    public string DisplayName { get; set; }

    public IScene Scene { get; }
    public IViewport Viewport { get; }
    public ICommandHistory History { get; }
    public ISelectionManager Selection { get; }
    public ILayerManager Layers { get; }
    public ISnapManager Snap { get; }
    public ICadEventBus EventBus { get; }

    public bool IsDirty { get; set; }

    /// <summary>
    /// Создать workspace с собственной шиной событий и новым Camera2D.
    /// </summary>
    public Workspace(WorkspaceId id)
        : this(id, new Camera2D(), new CadEventBus())
    {
    }

    /// <summary>
    /// Создать workspace с заданными viewport и шиной событий (для тестов и DI).
    /// </summary>
    public Workspace(WorkspaceId id, IViewport viewport, ICadEventBus eventBus)
    {
        if (id.Id == Guid.Empty)
            throw new ArgumentException("WorkspaceId must have non-empty Guid.", nameof(id));

        Id = id;
        DisplayName = id.DisplayName;
        Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
        EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        // Создаём Scene (использует EventBus)
        Scene = new SceneImpl(eventBus);

        // Layers — управляет теми же слоями, что и Scene.Layers
        Layers = new LayerManager(eventBus, Scene.Layers);

        // Selection — управляет выделением объектов Scene
        Selection = new SelectionManager(Scene, eventBus);

        // History — пустая, Commands регистрируются по мере необходимости
        // Используем фасад ICommandHistory (scene-агностик)
        History = new WorkspaceHistoryFacade();

        // Snap — скелетный менеджер
        Snap = new SnapManager();

        if (Viewport is Camera2D camera)
        {
            camera.SelectionBoundsResolver = ResolveSelectionBounds;
        }
    }

    /// <summary>Масштабирует вид по текущему выделению сцены.</summary>
    public void ZoomToSelection()
    {
        if (!Selection.HasSelection)
        {
            return;
        }

        Viewport.ZoomToSelection(Selection.SelectedIds);
    }

    private Bounds2D? ResolveSelectionBounds(IEnumerable<Guid> objectIds)
    {
        Bounds2D? bounds = null;
        foreach (var objectId in objectIds)
        {
            var obj = Scene.Find(objectId);
            if (obj is null || obj.Bounds.IsEmpty)
            {
                continue;
            }

            bounds = bounds is null ? obj.Bounds : bounds.Value.Union(obj.Bounds);
        }

        return bounds;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Scene.Clear();
        Selection.Clear();
        _disposed = true;
    }

    /// <summary>
    /// Фасад <see cref="ICommandHistory"/> для workspace: отделяет команды
    /// от конкретного типа контекста. Полная интеграция — при миграции редактора.
    /// </summary>
    private sealed class WorkspaceHistoryFacade : ICommandHistory
    {
        public bool CanUndo => false;
        public bool CanRedo => false;
        public int UndoCount => 0;
        public int RedoCount => 0;
        public int MaxSize { get; set; } = CommandHistory<object>.DefaultMaxSize;

        public void Undo() { }
        public void Redo() { }
        public void Clear() { }

        public IReadOnlyList<string> GetUndoNames() => Array.Empty<string>();
        public IReadOnlyList<string> GetRedoNames() => Array.Empty<string>();

        // Reserved for future use: raised when undo/redo stack integration is completed.
#pragma warning disable CS0067
        public event EventHandler? HistoryChanged;
#pragma warning restore CS0067
    }
}
