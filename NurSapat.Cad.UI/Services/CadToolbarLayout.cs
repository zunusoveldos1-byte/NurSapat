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
    string IconResourceKey,
    CadToolKind? CadToolKind = null,
    bool IsImplemented = true,
    bool IsInstantCommand = false);

/// <summary>Статический расклад: создание, редактирование и декорирование.</summary>
public static class CadToolbarLayout
{
    public const string BlockCreation = "СОЗДАНИЕ";
    public const string BlockEditing = "РЕДАКТИРОВАНИЕ";
    public const string BlockDecoration = "ДЕКОРИРОВАНИЕ";

    private static readonly IReadOnlyList<CadToolbarToolSpec> CreationTools =
    [
        Tool("frame-real", "Коробка", WindowToolMode.RealFrame, "Icon.RealFrame"),
        Tool("frame-fake", "Коробка фиктивная", WindowToolMode.FakeFrame, "Icon.FakeFrame"),
        Tool("impost-fake-v", "Импост фиктивный вертикальный", WindowToolMode.FakeVerticalImpost, "Icon.VerticalImpost"),
        Tool("impost-fake-h", "Импост фиктивный горизонтальный", WindowToolMode.FakeHorizontalImpost, "Icon.HorizontalImpost"),
        Tool("arch", "Арочная коробка", WindowToolMode.FakeFrameArch, "Icon.FakeArch"),
        Tool("mullion-edge", "Стойка/ригель в крайнем положении", WindowToolMode.ExtremeImpost, "Icon.MullionEdge"),
        Tool("frame-fake-circle", "Коробка фиктивная круглая", WindowToolMode.FakeFrameCircle, "Icon.FakeCircle"),
        Tool("frame-fake-triangle", "Коробка фиктивная треугольная", WindowToolMode.FakeFrameTriangle, "Icon.FakeTriangle"),
        Tool("adapter-profile", "Переходной профиль", WindowToolMode.AdapterProfile, "Icon.AdapterProfile"),
        Tool("sash", "Створка", WindowToolMode.Sash, "Icon.Sash"),
        Tool("hardware", "Фурнитура", WindowToolMode.Hardware, "Icon.Hardware"),
        Tool("handle", "Ручка", WindowToolMode.Handle, "Icon.Handle"),
        Tool("impost-h", "Импост горизонтальный", WindowToolMode.RealHorizontalImpost, "Icon.RealHorizontalImpost"),
        Tool("impost-v", "Импост вертикальный", WindowToolMode.RealVerticalImpost, "Icon.RealVerticalImpost"),
        Tool("impost-double-h", "Импост двойной горизонтальный", WindowToolMode.DoubleHorizontalImpost, "Icon.DoubleHorizontalImpost"),
        Tool("impost-double-v", "Импост двойной вертикальный", WindowToolMode.DoubleVerticalImpost, "Icon.DoubleVerticalImpost"),
        Tool("impost-slanted", "Импост наклонный", WindowToolMode.SlantedImpost, "Icon.SlantedImpost"),
        Tool("corrugation-v", "Гофра вертикальная", WindowToolMode.CorrugationVertical, "Icon.CorrugationVertical"),
        Tool("corrugation-h", "Гофра горизонтальная", WindowToolMode.CorrugationHorizontal, "Icon.CorrugationHorizontal"),
        Tool("corrugation-slanted", "Гофра наклонная", WindowToolMode.Corrugation, "Icon.CorrugationSlanted", implemented: false),
        Tool("glazing", "Остекление", WindowToolMode.Glassing, "Icon.Glassing"),
        Tool("sill", "Подоконник", WindowToolMode.Sill, "Icon.Sill"),
        Tool("bead", "Штапик", WindowToolMode.BeadSystem, "Icon.Bead", implemented: false),
        Tool("lock", "Замок", WindowToolMode.Other, "Icon.Lock", implemented: false),
        Tool("opening-limiter", "Ограничитель открывания", WindowToolMode.Other, "Icon.Limiter", implemented: false),
        Tool("door-closer", "Доводчик", WindowToolMode.Other, "Icon.Closer", implemented: false),
        Tool("espagnolette", "Шпингалет", WindowToolMode.Other, "Icon.Espagnolette", implemented: false),
        Tool("mosquito-net", "Москитная сетка", WindowToolMode.MosquitoNet, "Icon.MosquitoNet"),
        Tool("sash-fixed", "Створка глухая", WindowToolMode.Other, "Icon.SashFixed", implemented: false),
        Tool("sash-turn", "Створка поворотная", WindowToolMode.Other, "Icon.SashTurn", implemented: false),
        Tool("sash-tilt", "Створка откидная", WindowToolMode.Other, "Icon.SashTilt", implemented: false),
        Tool("sash-suspended", "Створка подвисная", WindowToolMode.Other, "Icon.SashSuspended", implemented: false),
        Tool("sash-turn-v", "Створка поворотная по вертикальной оси", WindowToolMode.Other, "Icon.SashTurnVertical", implemented: false),
        Tool("sash-turn-h", "Створка поворотная по горизонтальной оси", WindowToolMode.Other, "Icon.SashTurnHorizontal", implemented: false),
        Tool("sash-sliding", "Створка раздвижная", WindowToolMode.SlidingSash, "Icon.SashSliding", implemented: false),
        Tool("sash-folding", "Створка складная", WindowToolMode.Other, "Icon.SashFolding", implemented: false),
        Tool("additional-elements", "Дополнительные элементы", WindowToolMode.AdditionalElement, "Icon.More", implemented: false),
        Tool("cornice", "Карниз", WindowToolMode.Other, "Icon.Cornice", implemented: false),
        Tool("blind-control", "Блок управления", WindowToolMode.Other, "Icon.BlindControl", implemented: false),
        Tool("blind-v", "Жалюзи вертикальные", WindowToolMode.Other, "Icon.BlindVertical", implemented: false),
        Tool("blind-h", "Жалюзи горизонтальные", WindowToolMode.Other, "Icon.BlindHorizontal", implemented: false)
    ];

        private static readonly IReadOnlyList<CadToolbarToolSpec> EditingTools =
        [
            Tool("select-elements", "Выделение элемента", WindowToolMode.SelectElement, "Icon.Selection", CadToolKind.SelectElements),
            Tool("delete-last", "Удалить последний установленный элемент", WindowToolMode.Other, "Icon.UndoLast", CadToolKind.DeleteLastPlaced, isInstant: true),
            Tool("undo", "Отмена действия", WindowToolMode.Other, "Icon.UndoGlobal", CadToolKind.Undo, isInstant: true),
            Tool("redo", "Вернуть", WindowToolMode.Other, "Icon.RedoGlobal", isInstant: true),
            Tool("select-object", "Выделение объекта", WindowToolMode.Select, "Icon.SelectObject", CadToolKind.SelectObject),
            Tool("select-profile", "Выделение профиля", WindowToolMode.SelectProfile, "Icon.SelectProfile", CadToolKind.SelectProfile),
            Tool("select-vertex", "Выделение вершины", WindowToolMode.SelectVertex, "Icon.SelectVertex", CadToolKind.SelectVertex),
            Tool("align-glazing", "Выравнивание стеклопакетов", WindowToolMode.AlignGlazingUnits, "Icon.AlignGlassUnits", CadToolKind.AlignGlazingUnits, isInstant: true, implemented: false),
            Tool("align-dead-lights", "Выравнивание глухих просветов", WindowToolMode.AlignDeadLights, "Icon.AlignClearOpenings", CadToolKind.AlignDeadLights, isInstant: true, implemented: false),
            Tool("set-vertex-coordinates", "Установить координаты вершины", WindowToolMode.Other, "Icon.SelectVertex", CadToolKind.SetVertexCoordinates, isInstant: true, implemented: false),
            Tool("split-profile", "Разбить профиль пополам", WindowToolMode.Other, "Icon.Resize", CadToolKind.SplitProfile, isInstant: true, implemented: false),
            Tool("offset-impost-h", "Сместить импост по горизонтали", WindowToolMode.OffsetImpostHorizontal, "Icon.MoveMullionHorizontal", CadToolKind.OffsetImpostHorizontal),
            Tool("offset-impost-v", "Сместить импост по вертикали", WindowToolMode.OffsetImpostVertical, "Icon.MoveMullionVertical", CadToolKind.OffsetImpostVertical),
            Tool("align-imposts", "Выравнивание импостов", WindowToolMode.AlignImposts, "Icon.AlignMullions", CadToolKind.AlignImposts, isInstant: true, implemented: false),
            Tool("arc-vertex", "Выделение дуговых вершин", WindowToolMode.Other, "Icon.ArcVertex", implemented: false),
            Tool("radius-vertex", "Выделение радиусных вершин", WindowToolMode.RadiusVertex, "Icon.RadiusVertex", CadToolKind.RadiusVertex),
            Tool("remove-arc-vertex", "Убрать дуговые/радиусные вершины", WindowToolMode.Other, "Icon.RemoveArcVertex", implemented: false),
            Tool("split-arc", "Разбить арку на сегменты", WindowToolMode.Other, "Icon.SplitArc", implemented: false),
            Tool("set-radius", "Задать радиус", WindowToolMode.Other, "Icon.SetRadius", implemented: false),
            Tool("set-arch-height", "Задать высоту арки", WindowToolMode.Other, "Icon.SetArchHeight", isInstant: true),
            Tool("zoom", "Масштаб", WindowToolMode.ZoomView, "Icon.Zoom"),
            Tool("pan", "Перемещение вида", WindowToolMode.PanView, "Icon.Hand"),
            Tool("zoom-fit", "Автомасштаб", WindowToolMode.Other, "Icon.ZoomFit", isInstant: true),
            Tool("recalculate", "Пересчитать конструкцию", WindowToolMode.Other, "Icon.Recalculate", isInstant: true, implemented: false),
            Tool("properties", "Свойства объекта", WindowToolMode.Other, "Icon.Properties", implemented: false),
            Tool("delete-selection", "Удаление", WindowToolMode.Other, "Icon.Trash", CadToolKind.DeleteSelection, isInstant: true),
            Tool("clear-construction", "Очистить всю конструкцию", WindowToolMode.Other, "Icon.ClearAll", CadToolKind.ClearConstruction, isInstant: true),
            Tool("copy-construction", "Копировать конструкцию", WindowToolMode.Other, "Icon.Copy", isInstant: true, implemented: false),
            Tool("paste-construction", "Вставить конструкцию", WindowToolMode.Other, "Icon.Paste", isInstant: true, implemented: false),
            Tool("details", "Детали", WindowToolMode.Other, "Icon.Details", isInstant: true, implemented: false),
            Tool("add-to-standard", "Добавить в «Типовые конструкции»", WindowToolMode.Other, "Icon.AddToStandard", isInstant: true, implemented: false),
            Tool("insert-from-standard", "Вставить из «Типовых конструкций»", WindowToolMode.Other, "Icon.InsertFromStandard", isInstant: true, implemented: false)
        ];

        private static readonly IReadOnlyList<CadToolbarToolSpec> DecorationTools =
        [
            Tool("decorative-spoke", "Декоративная шпроса", WindowToolMode.DecorativeSpoke, "Icon.DecorativeSpoke")
        ];

    public static IReadOnlyList<(string BlockTitle, IReadOnlyList<CadToolbarToolSpec> Tools)> Blocks { get; } =
    [
        (BlockCreation, CreationTools),
        (BlockEditing, EditingTools),
        (BlockDecoration, DecorationTools)
    ];

    private static CadToolbarToolSpec Tool(
        string key,
        string title,
        WindowToolMode mode,
        string iconKey,
        CadToolKind? cadToolKind = null,
        bool isInstant = false,
        bool implemented = true) =>
        new(key, title, mode, iconKey, cadToolKind, implemented, isInstant);

    public static void Populate(
        ICollection<CadDesignToolButtonViewModel> creation,
        ICollection<CadDesignToolButtonViewModel> editing,
        ICollection<CadDesignToolButtonViewModel> decoration,
        IRelayCommand<string> activateCommand)
    {
        PopulateBlock(creation, CreationTools, activateCommand);
        PopulateBlock(editing, EditingTools, activateCommand);
        PopulateBlock(decoration, DecorationTools, activateCommand);
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
                iconResourceKey: spec.IconResourceKey,
                activateCommand: activateCommand,
                cadToolKind: spec.CadToolKind,
                isInstantCommand: spec.IsInstantCommand,
                isImplemented: spec.IsImplemented));
        }
    }
}
