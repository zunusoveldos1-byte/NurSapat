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

    // Д) Элементы
    /// <summary>Переходной профиль.</summary>
    AdapterProfile,

    /// <summary>Ручка.</summary>
    Handle,

    // Системные
    /// <summary>Разделитель панели инструментов.</summary>
    Separator,

    /// <summary>Стойка/ригель: нет.</summary>
    RackRigelNone
}
