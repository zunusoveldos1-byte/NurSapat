namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>Стабильный идентификатор оси сетки в пределах проекции FrameRoot.</summary>
public readonly record struct GridLineId(Guid Value)
{
    public static GridLineId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
