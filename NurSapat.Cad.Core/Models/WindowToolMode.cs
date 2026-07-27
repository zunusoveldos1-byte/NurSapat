namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Режимы инструментов чертёжной вкладки (реестр Ccalc speedbtnclick).
/// </summary>
public enum WindowToolMode
{
    Other = 0,
    Select,
    SelectProfile,
    SelectVertex,
    SelectElement,
    ZoomView,
    PanView,
    Frame,
    VerticalImpost,
    HorizontalImpost,
    DiagonalImpost,
    MullionVertical,
    MullionHorizontal,
    Post,
    Glass,
    GlassWithBead,
    Sash,
    ShprossVertical,
    ShprossHorizontal,
    BeadSystem,
    AdditionalElement,
    Corrugation,
    /// <summary>Гофра вертикальная (заполнение проёма).</summary>
    CorrugationVertical,
    /// <summary>Гофра горизонтальная (заполнение проёма).</summary>
    CorrugationHorizontal,
    /// <summary>Гофра наклонная 45° (заполнение проёма).</summary>
    CorrugationSlanted,
    Hardware,
    SlidingSash,
    RoundDummyFrame,
    FictitiousVerticalImpost,
    FictitiousHorizontalImpost,
    ClearConstruction,
    AutoScale,
    KonAdd,
    KonDel,

    // А) Коробки
    /// <summary>Габаритная фиктивная коробка (bounding frame) для составных конструкций.</summary>
    FictitiousFrame,

    /// <summary>Коробка фиктивная — автоматический ввод размеров.</summary>
    FakeFrame,

    /// <summary>Коробка (рама) — автоматический ввод размеров.</summary>
    RealFrame,

    /// <summary>Коробка фиктивная круглая.</summary>
    FakeFrameCircle,

    /// <summary>Коробка фиктивная треугольная.</summary>
    FakeFrameTriangle,

    /// <summary>Полукруг фиктивный.</summary>
    FakeFrameArch,

    // Б) Импосты одинарные (сплошные)
    /// <summary>Импост фиктивный вертикальный.</summary>
    FakeVerticalImpost,

    /// <summary>Импост фиктивный горизонтальный.</summary>
    FakeHorizontalImpost,

    /// <summary>Импост сплошной вертикальный.</summary>
    RealVerticalImpost,

    /// <summary>Импост сплошной горизонтальный.</summary>
    RealHorizontalImpost,

    // В) Импосты двойные
    /// <summary>Импост двойной вертикальный.</summary>
    DoubleVerticalImpost,

    /// <summary>Импост двойной горизонтальный.</summary>
    DoubleHorizontalImpost,

    // Г) Импост наклонный
    /// <summary>Импост наклонный.</summary>
    SlantedImpost,

    /// <summary>Остекление светового проёма.</summary>
    Glassing,

    /// <summary>Москитная сетка на створке.</summary>
    MosquitoNet,

    /// <summary>Подоконник у нижнего профиля рамы.</summary>
    Sill,

    /// <summary>Декоративные шпросы на стеклопакете.</summary>
    DecorativeSpoke,

    // Д) Элементы
    /// <summary>Переходной профиль.</summary>
    AdapterProfile,

    /// <summary>Ручка.</summary>
    Handle,

    /// <summary>Стойка/ригель в крайнем положении (у внутреннего края проёма).</summary>
    ExtremeImpost,

    // CAD-редактирование (панель инструментов)
    /// <summary>Сместить импост вертикально.</summary>
    OffsetImpostVertical,

    /// <summary>Сместить импост горизонтально.</summary>
    OffsetImpostHorizontal,

    /// <summary>Выравнивание импостов.</summary>
    AlignImposts,

    /// <summary>Выравнивание стеклопакетов.</summary>
    AlignGlazingUnits,

    /// <summary>Выравнивание глухих просветов.</summary>
    AlignDeadLights,

    /// <summary>Выделение радиусных вершин (преобразование верхнего профиля в дугу).</summary>
    RadiusVertex,

    /// <summary>Выделение и редактирование дуговых вершин арочного профиля.</summary>
    ArcVertex,

    /// <summary>Разделение арочного профиля вертикальным импостом.</summary>
    SplitArc,

    /// <summary>Замок на створке.</summary>
    Lock,

    /// <summary>Ограничитель открывания на створке.</summary>
    OpeningLimiter,

    /// <summary>Доводчик на двери/створке.</summary>
    DoorCloser,

    /// <summary>Шпингалет на створке.</summary>
    Espagnolette,

    /// <summary>Карниз по стороне конструкции.</summary>
    Cornice,

    /// <summary>Жалюзи вертикальные.</summary>
    BlindVertical,

    /// <summary>Жалюзи горизонтальные.</summary>
    BlindHorizontal,

    /// <summary>Блок управления жалюзи.</summary>
    BlindControl,

    // Системные
    /// <summary>Разделитель панели инструментов.</summary>
    Separator,

    /// <summary>Стойка/ригель: нет.</summary>
    RackRigelNone
}
