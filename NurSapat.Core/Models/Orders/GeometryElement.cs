using System.Text.Json.Serialization;

namespace NurSapat.Core.Models.Orders;

public enum GeometryElementKind
{
    /// <summary>Габаритная фиктивная коробка (bounding frame).</summary>
    FictitiousFrame,

    DummyFrame,
    DummyMullion,
    Frame,
    EdgeMullion,
    RoundDummyFrame,
    TransitionFrame,
    Sash,
    Hardware,
    Handle,
    Mullion,
    Corrugation,
    Glazing,
    Bead,
    MosquitoNet,
    Sill,
    SlidingSash,
    Installation,
    Accessory,
    ArchFrame,
    ArchDummyFrame,
    TriangleDummyFrame,
    DecorativeSpoke
}

public enum SashOpeningType
{
    Fixed,
    /// <summary>Поворотное (петли по HingeSide; для боковых — треугольник без откидной линии).</summary>
    Turn,
    Tilt,
    TiltTurn,
    SlideLeft,
    SlideRight,
    Suspended,
    Folding,
    /// <summary>Поворотное левое: петли слева, ручка справа, основание треугольника слева.</summary>
    LeftTurn,
    /// <summary>Поворотное правое: петли справа, ручка слева, основание треугольника справа.</summary>
    RightTurn
}

public sealed class GeometryElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public GeometryElementKind Kind { get; set; }

    public string Label { get; set; } = "";

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public SashOpeningType? OpeningType { get; set; }

    public SashHingeSide? HingeSide { get; set; }

    public bool HasHardware { get; set; }

    public bool HasHandle { get; set; }

    /// <summary>Москитная сетка установлена на створке.</summary>
    public bool HasMosquitoNet { get; set; }

    /// <summary>
    /// Штапик установлен на остеклении/в проёме.
    /// Управляет отрисовкой угловых стыков 45°: при <c>true</c> косые линии не рисуются.
    /// </summary>
    public bool HasBead { get; set; }

    public Guid? HostSashId { get; set; }

    /// <summary>Стеклопакет-родитель для декоративных шпросов.</summary>
    public Guid? ParentGlazingId { get; set; }

    /// <summary>Штапик-родитель для остекления, установленного через инструмент «Штапик».</summary>
    public Guid? ParentBeadId { get; set; }

    /// <summary>Стеклопакет, связанный с штапиком (на элементе штапика).</summary>
    public Guid? LinkedGlazingId { get; set; }

    /// <summary>Материал стекла (на элементе штапика).</summary>
    public Guid? GlassMaterialId { get; set; }

    /// <summary>Смещение штапика от светового проёма, мм.</summary>
    public double? BeadOffsetMm { get; set; }

    /// <summary>Время создания элемента (UTC).</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    public DecorativeSpokeType? SpokeType { get; set; }

    public double? SpokeThicknessMm { get; set; }

    public string? SpokeColor { get; set; }

    public int? SpokeRowsCount { get; set; }

    public int? SpokeColumnsCount { get; set; }

    public int? SpokeRayCount { get; set; }

    public bool SpokeIsArched { get; set; }

    public decimal? SpokeArcCenterX { get; set; }

    public decimal? SpokeArcCenterY { get; set; }

    public double? SpokeArcRadius { get; set; }

    public Guid? ProfileSeriesId { get; set; }

    public Guid? MaterialId { get; set; }

    /// <summary>
    /// Элемент участвует в списании материалов и расчёте объёма.
    /// false — при отсутствии материала или для фиктивных контейнеров.
    /// </summary>
    [JsonIgnore]
    public bool IsBillable =>
        MaterialId.HasValue && !FictitiousFrame.IsFictitiousElement(this);

    /// <summary>Id инструмента панели (mullion-v, slide-left и т.д.) для варианта отрисовки.</summary>
    public string? SourceToolId { get; set; }

    public decimal? EndX { get; set; }

    public decimal? EndY { get; set; }

    /// <summary>Крайний импост внутри проёма (без деления ячейки).</summary>
    public bool IsExtremeImpost { get; set; }

    public ImpostEdgeAlignment? EdgeAlignment { get; set; }

    public double? ExtremeImpostOffsetMm { get; set; }

    /// <summary>Идентификатор листа FrameNode, в котором установлен крайний импост.</summary>
    public Guid? HostFrameNodeId { get; set; }

    /// <summary>Абсолютная GridLine для переходного профиля.</summary>
    public Guid? HostGridLineId { get; set; }

    /// <summary>Владелец-подрама в ConstructionAssembly.</summary>
    public Guid? SubFrameId { get; set; }

    public Guid? ExtremeImpostSlotId { get; set; }

    /// <summary>Тип заполнения (стекло, гофра) для остекления и панелей гофры.</summary>
    public GlazingType? FillType { get; set; }

    /// <summary>Форма коробки для нестандартных конструкций.</summary>
    public ConstructionShape? Shape { get; set; }

    [JsonIgnore]
    public bool IsSelected { get; set; }
}
