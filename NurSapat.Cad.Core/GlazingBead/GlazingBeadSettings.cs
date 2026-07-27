namespace NurSapat.Cad.Core.GlazingBead;

/// <summary>Параметры инструмента «Штапик».</summary>
public static class GlazingBeadSettings
{
    /// <summary>Технологический зазор между стеклом и внутренней гранью штапика, мм.</summary>
    public const double TechnologyGapMm = 2.0;

    /// <summary>Ширина штапика по умолчанию, мм (если материал не задан).</summary>
    public const double DefaultBeadWidthMm = 18.0;

    /// <summary>Минимальный размер светового проёма для установки штапика, мм.</summary>
    public const double MinOpeningSizeMm = 40.0;
}
