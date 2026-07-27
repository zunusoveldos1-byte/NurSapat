using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Models;

/// <summary>
/// Временный «призрак» фурнитуры при наведении / выборе направления открывания.
/// HingeSide здесь — уже согласованный снимок из <c>SashOpeningState.Resolve</c>,
/// а не отдельный кэш стороны петель.
/// </summary>
public sealed class SashHardwareDragPreview
{
    public Guid SashId { get; init; }

    public double OriginX { get; init; }

    public double OriginY { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    public SashOpeningType OpeningType { get; init; }

    /// <summary>Совпадает с TriangleSide / GhostHingeSide из единого Resolve.</summary>
    public SashHingeSide HingeSide { get; init; }

    /// <summary>Следующий клик снимет фурнитуру — показываем полупрозрачные текущие маркеры.</summary>
    public bool WillClear { get; init; }
}
