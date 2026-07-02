using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Services;

/// <summary>Описание кнопки панели инструментов CAD.</summary>
public sealed record CadToolbarToolSpec(
    string ToolKey,
    string Title,
    WindowToolMode Mode,
    DesignToolIcon Icon,
    CadToolKind? CadToolKind = null,
    string? Mdl2Glyph = null,
    bool IsImplemented = true,
    bool IsInstantCommand = false);

/// <summary>Двухблочная панель: СОЗДАНИЕ и РЕДАКТИРОВАНИЕ.</summary>
public static class CadToolbarLayout
{
    public const string BlockCreation = "СОЗДАНИЕ";
    public const string BlockEditing = "РЕДАКТИРОВАНИЕ";

    private static readonly IReadOnlyList<CadToolbarToolSpec> CreationTools =
    [
        Tool("frame-real", "Коробка", WindowToolMode.RealFrame, DesignToolIcon.RealFrame),
        Tool("frame-fake", "Коробка фиктивная", WindowToolMode.FakeFrame, DesignToolIcon.FakeFrame),
        Tool("impost-fake-v", "Импост фиктивный вертикальный", WindowToolMode.FakeVerticalImpost, DesignToolIcon.VerticalImpost),
        Tool("impost-fake-h", "Импост фиктивный горизонтальный", WindowToolMode.FakeHorizontalImpost, DesignToolIcon.HorizontalImpost),
        Tool("arch", "Арочная коробка", WindowToolMode.FakeFrameArch, DesignToolIcon.FakeArch),
        Tool("mullion-edge", "Стойка/ригель в крайнем положении", WindowToolMode.Other, DesignToolIcon.MullionEdge, mdl2: "\uE8A5", implemented: false),
        Tool("frame-fake-circle", "Коробка фиктивная круглая", WindowToolMode.FakeFrameCircle, DesignToolIcon.FakeCircle),
        Tool("frame-fake-triangle", "Коробка фиктивная треугольная", WindowToolMode.FakeFrameTriangle, DesignToolIcon.FakeTriangle),
        Tool("adapter-profile", "Переходной профиль", WindowToolMode.AdapterProfile, DesignToolIcon.AdapterProfile),
        Tool("sash", "Створка", WindowToolMode.Sash, DesignToolIcon.Sash),
        Tool("hardware", "Фурнитура", WindowToolMode.Hardware, DesignToolIcon.Hardware),
        Tool("handle", "Ручка", WindowToolMode.Handle, DesignToolIcon.Handle),
        Tool("impost-h", "Импост горизонтальный", WindowToolMode.RealHorizontalImpost, DesignToolIcon.RealHorizontalImpost),
        Tool("impost-v", "Импост вертикальный", WindowToolMode.RealVerticalImpost, DesignToolIcon.RealVerticalImpost),
        Tool("impost-double-h", "Импост двойной горизонтальный", WindowToolMode.DoubleHorizontalImpost, DesignToolIcon.DoubleHorizontalImpost),
        Tool("impost-double-v", "Импост двойной вертикальный", WindowToolMode.DoubleVerticalImpost, DesignToolIcon.DoubleVerticalImpost),
        Tool("impost-slanted", "Импост наклонный", WindowToolMode.SlantedImpost, DesignToolIcon.SlantedImpost),
        Tool("corrugation-v", "Гофра вертикальная", WindowToolMode.Corrugation, DesignToolIcon.CorrugationVertical, mdl2: "\uE8A4", implemented: false),
        Tool("corrugation-h", "Гофра горизонтальная", WindowToolMode.Corrugation, DesignToolIcon.CorrugationHorizontal, mdl2: "\uE8A3", implemented: false),
        Tool("corrugation-slanted", "Гофра наклонная", WindowToolMode.Corrugation, DesignToolIcon.CorrugationSlanted, mdl2: "\uE8AC", implemented: false),
        Tool("glazing", "Остекление", WindowToolMode.Glassing, DesignToolIcon.Glassing),
        Tool("bead", "Штапик", WindowToolMode.BeadSystem, DesignToolIcon.Bead, mdl2: "\uE8B1", implemented: false),
        Tool("lock", "Замок", WindowToolMode.Other, DesignToolIcon.Lock, mdl2: "\uE72E", implemented: false),
        Tool("opening-limiter", "Ограничитель открывания", WindowToolMode.Other, DesignToolIcon.Limiter, mdl2: "\uE7BA", implemented: false),
        Tool("door-closer", "Доводчик", WindowToolMode.Other, DesignToolIcon.Closer, mdl2: "\uE7C5", implemented: false),
        Tool("espagnolette", "Шпингалет", WindowToolMode.Other, DesignToolIcon.Espagnolette, mdl2: "\uE8D7", implemented: false),
        Tool("mosquito-net", "Москитная сетка", WindowToolMode.Other, DesignToolIcon.MosquitoNet, mdl2: "\uE771", implemented: false),
        Tool("sash-fixed", "Створка глухая", WindowToolMode.Other, DesignToolIcon.SashFixed, mdl2: "\uE7C3", implemented: false),
        Tool("sash-turn", "Створка поворотная", WindowToolMode.Other, DesignToolIcon.SashTurn, mdl2: "\uE7C3", implemented: false),
        Tool("sash-tilt", "Створка откидная", WindowToolMode.Other, DesignToolIcon.SashTilt, mdl2: "\uE7C3", implemented: false),
        Tool("sash-suspended", "Створка подвисная", WindowToolMode.Other, DesignToolIcon.SashSuspended, mdl2: "\uE7C3", implemented: false),
        Tool("sash-turn-v", "Створка поворотная по вертикальной оси", WindowToolMode.Other, DesignToolIcon.SashTurnVertical, mdl2: "\uE7C3", implemented: false),
        Tool("sash-turn-h", "Створка поворотная по горизонтальной оси", WindowToolMode.Other, DesignToolIcon.SashTurnHorizontal, mdl2: "\uE7C3", implemented: false),
        Tool("sash-sliding", "Створка раздвижная", WindowToolMode.SlidingSash, DesignToolIcon.SashSliding, mdl2: "\uE7C3", implemented: false),
        Tool("sash-folding", "Створка складная", WindowToolMode.Other, DesignToolIcon.SashFolding, mdl2: "\uE7C3", implemented: false),
        Tool("additional-elements", "Дополнительные элементы", WindowToolMode.AdditionalElement, DesignToolIcon.More, mdl2: "\uE712", implemented: false),
        Tool("cornice", "Карниз", WindowToolMode.Other, DesignToolIcon.Cornice, mdl2: "\uE8A9", implemented: false),
        Tool("blind-control", "Блок управления", WindowToolMode.Other, DesignToolIcon.BlindControl, mdl2: "\uE7F4", implemented: false),
        Tool("blind-v", "Жалюзи вертикальные", WindowToolMode.Other, DesignToolIcon.BlindVertical, mdl2: "\uE8A4", implemented: false),
        Tool("blind-h", "Жалюзи горизонтальные", WindowToolMode.Other, DesignToolIcon.BlindHorizontal, mdl2: "\uE8A3", implemented: false)
    ];

    private static readonly IReadOnlyList<CadToolbarToolSpec> EditingTools =
    [
        Tool("select-elements", "Выделение элемента", WindowToolMode.SelectElement, DesignToolIcon.Selection, CadToolKind.SelectElements),
        Tool("delete-last", "Удалить последний установленный элемент", WindowToolMode.Other, DesignToolIcon.UndoLast, CadToolKind.DeleteLastPlaced, isInstant: true),
        Tool("undo", "Отмена действия", WindowToolMode.Other, DesignToolIcon.UndoGlobal, CadToolKind.Undo, isInstant: true),
        Tool("select-object", "Выделение объекта", WindowToolMode.Select, DesignToolIcon.SelectObject, CadToolKind.SelectObject),
        Tool("select-profile", "Выделение профиля", WindowToolMode.SelectProfile, DesignToolIcon.SelectProfile, CadToolKind.SelectProfile),
        Tool("select-vertex", "Выделение вершины", WindowToolMode.SelectVertex, DesignToolIcon.SelectVertex, CadToolKind.SelectVertex),
        Tool("align-glazing", "Выравнивание стеклопакетов", WindowToolMode.AlignGlazingUnits, DesignToolIcon.AlignGlassUnits, CadToolKind.AlignGlazingUnits, isInstant: true, implemented: false),
        Tool("align-dead-lights", "Выравнивание глухих просветов", WindowToolMode.AlignDeadLights, DesignToolIcon.AlignClearOpenings, CadToolKind.AlignDeadLights, isInstant: true, implemented: false),
        Tool("set-vertex-coordinates", "Установить координаты вершины", WindowToolMode.Other, DesignToolIcon.SelectVertex, CadToolKind.SetVertexCoordinates, isInstant: true, implemented: false),
        Tool("split-profile", "Разбить профиль пополам", WindowToolMode.Other, DesignToolIcon.Resize, CadToolKind.SplitProfile, mdl2: "\uE8A6", isInstant: true, implemented: false),
        Tool("offset-impost-h", "Сместить импост по горизонтали", WindowToolMode.OffsetImpostHorizontal, DesignToolIcon.MoveMullionHorizontal, CadToolKind.OffsetImpostHorizontal),
        Tool("offset-impost-v", "Сместить импост по вертикали", WindowToolMode.OffsetImpostVertical, DesignToolIcon.MoveMullionVertical, CadToolKind.OffsetImpostVertical),
        Tool("align-imposts", "Выравнивание импостов", WindowToolMode.AlignImposts, DesignToolIcon.AlignMullions, CadToolKind.AlignImposts, isInstant: true, implemented: false),
        Tool("arc-vertex", "Выделение дуговых вершин", WindowToolMode.Other, DesignToolIcon.ArcVertex, mdl2: "\uE8B6", implemented: false),
        Tool("radius-vertex", "Выделение радиусных вершин", WindowToolMode.RadiusVertex, DesignToolIcon.RadiusVertex, CadToolKind.RadiusVertex),
        Tool("remove-arc-vertex", "Убрать дуговые/радиусные вершины", WindowToolMode.Other, DesignToolIcon.RemoveArcVertex, mdl2: "\uE894", implemented: false),
        Tool("split-arc", "Разбить арку на сегменты", WindowToolMode.Other, DesignToolIcon.SplitArc, mdl2: "\uE8B1", implemented: false),
        Tool("set-radius", "Задать радиус", WindowToolMode.Other, DesignToolIcon.SetRadius, mdl2: "\uE8B6", implemented: false),
        Tool("set-arch-height", "Задать высоту арки", WindowToolMode.Other, DesignToolIcon.SetArchHeight, mdl2: "\uE8B8", isInstant: true),
        Tool("zoom", "Масштаб", WindowToolMode.ZoomView, DesignToolIcon.Zoom),
        Tool("pan", "Перемещение вида", WindowToolMode.PanView, DesignToolIcon.Hand),
        Tool("zoom-fit", "Автомасштаб", WindowToolMode.Other, DesignToolIcon.ZoomFit, isInstant: true),
        Tool("recalculate", "Пересчитать конструкцию", WindowToolMode.Other, DesignToolIcon.Recalculate, mdl2: "\uE9F5", isInstant: true, implemented: false),
        Tool("properties", "Свойства объекта", WindowToolMode.Other, DesignToolIcon.Properties, mdl2: "\uE90F", implemented: false),
        Tool("delete-selection", "Удаление", WindowToolMode.Other, DesignToolIcon.Trash, CadToolKind.DeleteSelection, isInstant: true),
        Tool("clear-construction", "Очистить всю конструкцию", WindowToolMode.Other, DesignToolIcon.ClearAll, CadToolKind.ClearConstruction, isInstant: true),
        Tool("copy-construction", "Копировать конструкцию", WindowToolMode.Other, DesignToolIcon.Copy, mdl2: "\uE8C8", isInstant: true, implemented: false),
        Tool("paste-construction", "Вставить конструкцию", WindowToolMode.Other, DesignToolIcon.Paste, mdl2: "\uE8CB", isInstant: true, implemented: false),
        Tool("details", "Детали", WindowToolMode.Other, DesignToolIcon.Details, mdl2: "\uE8A0", isInstant: true, implemented: false),
        Tool("add-to-standard", "Добавить в «Типовые конструкции»", WindowToolMode.Other, DesignToolIcon.AddToStandard, mdl2: "\uE710", isInstant: true, implemented: false),
        Tool("insert-from-standard", "Вставить из «Типовых конструкций»", WindowToolMode.Other, DesignToolIcon.InsertFromStandard, mdl2: "\uE8B0", isInstant: true, implemented: false)
    ];

    public static IReadOnlyList<(string BlockTitle, IReadOnlyList<CadToolbarToolSpec> Tools)> Blocks { get; } =
    [
        (BlockCreation, CreationTools),
        (BlockEditing, EditingTools)
    ];

    private static CadToolbarToolSpec Tool(
        string key,
        string title,
        WindowToolMode mode,
        DesignToolIcon icon,
        CadToolKind? cadToolKind = null,
        string? mdl2 = null,
        bool isInstant = false,
        bool implemented = true) =>
        new(key, title, mode, icon, cadToolKind, mdl2, implemented, isInstant);

    public static void Populate(
        ICollection<CadDesignToolButtonViewModel> creation,
        ICollection<CadDesignToolButtonViewModel> editing,
        IRelayCommand<string> activateCommand)
    {
        PopulateBlock(creation, CreationTools, activateCommand);
        PopulateBlock(editing, EditingTools, activateCommand);
    }

    private static void PopulateBlock(
        ICollection<CadDesignToolButtonViewModel> target,
        IReadOnlyList<CadToolbarToolSpec> tools,
        IRelayCommand<string> activateCommand)
    {
        target.Clear();

        foreach (var spec in tools)
        {
            target.Add(new CadDesignToolButtonViewModel(
                spec.ToolKey,
                spec.Mode,
                spec.Title,
                spec.Icon,
                activateCommand: activateCommand,
                cadToolKind: spec.CadToolKind,
                isInstantCommand: spec.IsInstantCommand,
                mdl2Glyph: spec.Mdl2Glyph,
                isImplemented: spec.IsImplemented));
        }
    }
}
