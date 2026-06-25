# NurSapat.Cad.Foundation

Ядро CAD-редактора уровня AutoCAD / SolidWorks / Revit.

Содержит **инфраструктуру** редактора (Scene Graph, Layers, Selection, Viewport, Commands,
Rendering, Snapping, Events, Workspace, Services) — без привязки к конкретной геометрии,
инструментам, UI-фреймворку (Avalonia) или предметной области (окна/двери).

## Правила использования

1. **Foundation не ссылается ни на один проект решения** — это чистое ядро.
   `NurSapat.Cad.Core` ссылается на Foundation (однонаправленно), а не наоборот.
2. **Никакого Avalonia** в этом проекте. UI взаимодействует с ядром только через интерфейсы и DI.
3. **Renderer никогда не работает со Scene напрямую** — только с `RenderQueue`, который
   строится `RenderQueueBuilder`-ом из `Scene`.
4. **Undo/Redo оперирует `ObjectId` (Guid)** — идентичность объектов неизменна.
5. **Многодокументность**: каждый открытый документ имеет собственный `Workspace`.

## Соответствие ТЗ (18 компонентов)

| # | Компонент | Типы |
|---|-----------|------|
| 1 | Scene Graph | `Scene/Scene.cs`, `SceneObject`, `SceneObjectCollection` |
| 2 | Scene Objects | `Scene/SceneObject.cs` (ObjectId, Name, LayerId, Visible, Locked, Selected, ...) |
| 3 | ObjectId | `Guid` в `SceneObject` (неизменный) |
| 4 | Viewport | `Viewport/IViewport.cs`, `Camera2D` |
| 5 | Camera2D | `Viewport/Camera2D.cs` (база для Camera3D) |
| 6 | Selection Manager | `Selection/SelectionManager.cs` |
| 7 | History | `Commands/CommandHistory<T>.cs` |
| 8 | Command Infrastructure | `ICadCommand<T>`, `CadCommandBase<T>` |
| 9 | Render Queue | `Rendering/RenderQueue.cs`, `RenderQueueBuilder` |
| 10 | Renderer Abstraction | `IRenderer`, `IRenderContext`, `IRenderSurface` |
| 11 | Snap Infrastructure | `Snapping/ISnapManager.cs`, `SnapPoint` |
| 12 | Layer Manager | `Layers/LayerManager.cs` |
| 13 | Event System | `Events/CadEventBus.cs` + 8 событий |
| 14 | Workspace | `Workspace/Workspace.cs` |
| 15 | Multi Document | `Workspace/WorkspaceManager.cs` |
| 16 | Services | `Services/I*Service.cs` + реализации |
| 17 | DI | `Services/ServiceCollectionExtensions.AddCadFoundation()` |

## Мост с существующим кодом (обратно-совместимо)

- `NurSapat.Cad.Core.ICadCommand` наследует `ICadCommand<CadDocument>`.
- `NurSapat.Cad.Core.CadCommandManager` наследует `CommandHistory<CadDocument>`
  (получает `Clear()` и `MaxSize`).
- `NurSapat.Cad.Core.CadViewport` реализует `IViewport` (через композицию с `Camera2D`).
