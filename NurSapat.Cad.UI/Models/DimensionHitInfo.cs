using Avalonia;

namespace NurSapat.Cad.UI.Models;

public enum DimensionHitKind
{
    OverallWidth,
    OverallHeight,
    SegmentWidth
}

/// <summary>
/// Область клика по размерной надписи на чертеже и контекст для пересчёта геометрии.
/// </summary>
public sealed class DimensionHitInfo
{
    public DimensionHitKind Kind { get; init; }

    public double ValueMm { get; init; }

    /// <summary>Точные экранные границы надписи (без отступа для клика).</summary>
    public Rect TextBounds { get; init; }

    /// <summary>Расширенная область для hit-test.</summary>
    public Rect ScreenRect { get; init; }

    public bool IsVertical { get; init; }

    /// <summary>Индекс сегмента нижней цепочки размеров (0 — слева).</summary>
    public int SegmentIndex { get; init; }

    /// <summary>Мировые X границ сегментов: [левый торец, импосты…, правый торец].</summary>
    public IReadOnlyList<double>? BoundaryWorldX { get; init; }

    /// <summary>Импост, который нужно сдвинуть при изменении внутреннего сегмента.</summary>
    public VerticalImpostTarget? ImpostTarget { get; init; }

    /// <summary>true — сдвигать правую границу сегмента; false — левую (для крайнего правого сегмента).</summary>
    public bool MoveRightBoundary { get; init; }
}
