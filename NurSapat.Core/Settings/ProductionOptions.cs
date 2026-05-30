namespace NurSapat.Core.Settings;

public sealed class ProductionOptions
{
    public decimal WeldingAllowance { get; init; } = 3.00m;

    public decimal GlassGlazingGap { get; init; } = 5.00m;

    public decimal ImpostMillingAllowance { get; init; } = 6.00m;

    public decimal SashOverlap { get; init; } = 8.00m;
}
