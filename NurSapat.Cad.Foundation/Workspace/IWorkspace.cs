using NurSapat.Cad.Foundation.Commands;
using NurSapat.Cad.Foundation.Events;
using NurSapat.Cad.Foundation.Layers;
using NurSapat.Cad.Foundation.Scene;
using NurSapat.Cad.Foundation.Selection;
using NurSapat.Cad.Foundation.Snapping;
using NurSapat.Cad.Foundation.Viewport;

namespace NurSapat.Cad.Foundation.Workspace;

/// <summary>
/// Контракт рабочего пространства: агрегат, владеющий Scene, Viewport, History, Selection,
/// Layers и EventBus одного открытого документа.
/// </summary>
public interface IWorkspace : IDisposable
{
    WorkspaceId Id { get; }
    string DisplayName { get; set; }

    IScene Scene { get; }
    IViewport Viewport { get; }
    ICommandHistory History { get; }
    ISelectionManager Selection { get; }
    ILayerManager Layers { get; }
    ISnapManager Snap { get; }
    ICadEventBus EventBus { get; }

    /// <summary>Были ли несохранённые изменения.</summary>
    bool IsDirty { get; set; }
}
