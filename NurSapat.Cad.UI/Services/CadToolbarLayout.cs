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
        Tool("frame-real", "Коробка", WindowToolMode.RealFrame, "CadIcon.RealFrame"),
        Tool("frame-fake", "Коробка фиктивная", WindowToolMode.FakeFrame, "CadIcon.FakeFrame"),
        Tool("impost-fake-v", "Импост фиктивный вертикальный", WindowToolMode.FakeVerticalImpost, "CadIcon.VerticalImpost"),
        Tool("impost-fake-h", "Импост фиктивный горизонтальный", WindowToolMode.FakeHorizontalImpost, "CadIcon.HorizontalImpost"),
        Tool("arch", "Арочная коробка", WindowToolMode.FakeFrameArch, "CadIcon.FakeArch"),
        Tool("mullion-edge", "Стойка/ригель в крайнем положении", WindowToolMode.ExtremeImpost, "CadIcon.MullionEdge"),
        Tool("frame-fake-circle", "Коробка фиктивная круглая", WindowToolMode.FakeFrameCircle, "CadIcon.FakeCircle"),
        Tool("frame-fake-triangle", "Коробка фиктивная треугольная", WindowToolMode.FakeFrameTriangle, "CadIcon.FakeTriangle"),
        Tool("adapter-profile", "Переходной профиль", WindowToolMode.AdapterProfile, "CadIcon.AdapterProfile"),
        Tool("sash", "Створка", WindowToolMode.Sash, "CadIcon.Sash"),
        Tool("hardware", "Фурнитура", WindowToolMode.Hardware, "CadIcon.Hardware"),
        Tool("handle", "Ручка", WindowToolMode.Handle, "CadIcon.Handle"),
        Tool("impost-h", "Импост горизонтальный", WindowToolMode.RealHorizontalImpost, "CadIcon.RealHorizontalImpost"),
        Tool("impost-v", "Импост вертикальный", WindowToolMode.RealVerticalImpost, "CadIcon.RealVerticalImpost"),
        Tool("impost-double-h", "Импост двойной горизонтальный", WindowToolMode.DoubleHorizontalImpost, "CadIcon.DoubleHorizontalImpost"),
        Tool("impost-double-v", "Импост двойной вертикальный", WindowToolMode.DoubleVerticalImpost, "CadIcon.DoubleVerticalImpost"),
        Tool("impost-slanted", "Импост наклонный", WindowToolMode.SlantedImpost, "CadIcon.SlantedImpost"),
        Tool("corrugation-v", "Гофра вертикальная", WindowToolMode.CorrugationVertical, "CadIcon.CorrugationVertical"),
        Tool("corrugation-h", "Гофра горизонтальная", WindowToolMode.CorrugationHorizontal, "CadIcon.CorrugationHorizontal"),
        Tool("corrugation-slanted", "Гофра наклонная", WindowToolMode.CorrugationSlanted, "CadIcon.CorrugationSlanted"),
        Tool("glazing", "Остекление", WindowToolMode.Glassing, "CadIcon.Glassing"),
        Tool("sill", "Подоконник", WindowToolMode.Sill, "CadIcon.Sill"),
        Tool("bead", "Штапик", WindowToolMode.BeadSystem, "CadIcon.Bead"),
        Tool("lock", "Замок", WindowToolMode.Lock, "CadIcon.Lock"),
        Tool("opening-limiter", "Ограничитель открывания", WindowToolMode.OpeningLimiter, "CadIcon.Limiter"),
        Tool("door-closer", "Доводчик", WindowToolMode.DoorCloser, "CadIcon.Closer"),
        Tool("espagnolette", "Шпингалет", WindowToolMode.Espagnolette, "CadIcon.Espagnolette"),
        Tool("mosquito-net", "Москитная сетка", WindowToolMode.MosquitoNet, "CadIcon.MosquitoNet"),
        Tool("sash-fixed", "Створка глухая", WindowToolMode.Sash, "CadIcon.SashFixed"),
        Tool("sash-turn", "Створка поворотная", WindowToolMode.Sash, "CadIcon.SashTurn"),
        Tool("sash-tilt", "Створка откидная", WindowToolMode.Sash, "CadIcon.SashTilt"),
        Tool("sash-suspended", "Створка подвисная", WindowToolMode.Sash, "CadIcon.SashSuspended"),
        Tool("sash-turn-v", "Створка поворотная по вертикальной оси", WindowToolMode.Sash, "CadIcon.SashTurnVertical"),
        Tool("sash-turn-h", "Створка поворотная по горизонтальной оси", WindowToolMode.Sash, "CadIcon.SashTurnHorizontal"),
        Tool("sash-sliding", "Створка раздвижная", WindowToolMode.SlidingSash, "CadIcon.SashSliding"),
        Tool("sash-folding", "Створка складная", WindowToolMode.Sash, "CadIcon.SashFolding"),
        Tool("additional-elements", "Дополнительные элементы", WindowToolMode.AdditionalElement, "CadIcon.More"),
        Tool("cornice", "Карниз", WindowToolMode.Cornice, "CadIcon.Cornice"),
        Tool("blind-control", "Блок управления", WindowToolMode.BlindControl, "CadIcon.BlindControl"),
        Tool("blind-v", "Жалюзи вертикальные", WindowToolMode.BlindVertical, "CadIcon.MosquitoNet"),
        Tool("blind-h", "Жалюзи горизонтальные", WindowToolMode.BlindHorizontal, "CadIcon.CorrugationHorizontal")
    ];

        private static readonly IReadOnlyList<CadToolbarToolSpec> EditingTools =
        [
            Tool("select-elements", "Выделение элемента", WindowToolMode.SelectElement, "CadIcon.Selection", CadToolKind.SelectElements),
            Tool("delete-last", "Удалить последний установленный элемент", WindowToolMode.Other, "CadIcon.UndoLast", CadToolKind.DeleteLastPlaced, isInstant: true),
            Tool("undo", "Отмена действия", WindowToolMode.Other, "CadIcon.UndoGlobal", CadToolKind.Undo, isInstant: true),
            Tool("redo", "Вернуть", WindowToolMode.Other, "CadIcon.RedoGlobal", isInstant: true),
            Tool("select-object", "Выделение объекта", WindowToolMode.Select, "CadIcon.SelectObject", CadToolKind.SelectObject),
            Tool("select-profile", "Выделение профиля", WindowToolMode.SelectProfile, "CadIcon.SelectProfile", CadToolKind.SelectProfile),
            Tool("select-vertex", "Выделение вершины", WindowToolMode.SelectVertex, "CadIcon.SelectVertex", CadToolKind.SelectVertex),
            Tool("align-glazing", "Выравнивание стеклопакетов", WindowToolMode.AlignGlazingUnits, "CadIcon.AlignGlassUnits", CadToolKind.AlignGlazingUnits, isInstant: true),
            Tool("align-dead-lights", "Выравнивание глухих просветов", WindowToolMode.AlignDeadLights, "CadIcon.AlignClearOpenings", CadToolKind.AlignDeadLights, isInstant: true),
            Tool("set-vertex-coordinates", "Установить координаты вершины", WindowToolMode.Other, "CadIcon.SelectVertex", CadToolKind.SetVertexCoordinates),
            Tool("split-profile", "Разбить профиль пополам", WindowToolMode.Other, "CadIcon.Resize", CadToolKind.SplitProfile, isInstant: true),
            Tool("offset-impost-h", "Сместить импост по горизонтали", WindowToolMode.OffsetImpostHorizontal, "CadIcon.MoveMullionHorizontal", CadToolKind.OffsetImpostHorizontal),
            Tool("offset-impost-v", "Сместить импост по вертикали", WindowToolMode.OffsetImpostVertical, "CadIcon.MoveMullionVertical", CadToolKind.OffsetImpostVertical),
            Tool("align-imposts", "Выравнивание импостов", WindowToolMode.AlignImposts, "CadIcon.AlignMullions", CadToolKind.AlignImposts, isInstant: true),
            Tool("change-dimensions", "Изменить габаритные размеры", WindowToolMode.Other, "CadIcon.Resize", CadToolKind.ChangeOverallDimensions, isInstant: true),
            Tool("arc-vertex", "Выделение дуговых вершин", WindowToolMode.ArcVertex, "CadIcon.ArcVertex", CadToolKind.ArcVertex),
            Tool("radius-vertex", "Выделение радиусных вершин", WindowToolMode.RadiusVertex, "CadIcon.FakeArch", CadToolKind.RadiusVertex),
            Tool("remove-arc-vertex", "Убрать дуговые/радиусные вершины", WindowToolMode.Other, "CadIcon.RemoveArcVertex", CadToolKind.RemoveArcVertex, isInstant: true),
            Tool("split-arc", "Разбить арку на сегменты", WindowToolMode.SplitArc, "CadIcon.SplitArc", CadToolKind.SplitArc),
            Tool("set-radius", "Задать радиус", WindowToolMode.Other, "CadIcon.SetRadius", CadToolKind.SetRadius, isInstant: true),
            Tool("set-arch-height", "Задать высоту арки", WindowToolMode.Other, "CadIcon.SetArchHeight", isInstant: true),
            Tool("zoom", "Масштаб", WindowToolMode.ZoomView, "CadIcon.Zoom"),
            Tool("pan", "Перемещение вида", WindowToolMode.PanView, "CadIcon.Hand"),
            Tool("zoom-fit", "Автомасштаб", WindowToolMode.Other, "CadIcon.ZoomFit", isInstant: true),
            Tool("recalculate", "Пересчитать конструкцию", WindowToolMode.Other, "CadIcon.Recalculate", CadToolKind.Recalculate, isInstant: true),
            Tool("properties", "Свойства объекта", WindowToolMode.Other, "CadIcon.Properties", CadToolKind.Properties, isInstant: true),
            Tool("delete-selection", "Удаление", WindowToolMode.Other, "CadIcon.Trash", CadToolKind.DeleteSelection, isInstant: true),
            Tool("clear-construction", "Очистить всю конструкцию", WindowToolMode.Other, "CadIcon.ClearAll", CadToolKind.ClearConstruction, isInstant: true),
            Tool("copy-construction", "Копировать конструкцию", WindowToolMode.Other, "CadIcon.Copy", CadToolKind.CopyConstruction, isInstant: true),
            Tool("paste-construction", "Вставить конструкцию", WindowToolMode.Other, "CadIcon.Paste", CadToolKind.PasteConstruction, isInstant: true),
            Tool("details", "Детали", WindowToolMode.Other, "CadIcon.Details", CadToolKind.Details, isInstant: true),
            Tool("add-to-standard", "Добавить в «Типовые конструкции»", WindowToolMode.Other, "CadIcon.AddToStandard", CadToolKind.AddToStandard, isInstant: true),
            Tool("insert-from-standard", "Вставить из «Типовых конструкций»", WindowToolMode.Other, "CadIcon.InsertFromStandard", CadToolKind.InsertFromStandard, isInstant: true)
        ];

        private static readonly IReadOnlyList<CadToolbarToolSpec> DecorationTools =
        [
            Tool("decorative-spoke", "Декоративная шпроса", WindowToolMode.DecorativeSpoke, "CadIcon.DecorativeSpoke")
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
