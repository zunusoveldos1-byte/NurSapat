using NurSapat.Core.Settings;

namespace NurSapat.Core.Calculations;

public sealed class CalculationEngine
{
    private readonly ProductionOptions _options;

    public CalculationEngine(ProductionOptions? options = null)
    {
        _options = options ?? new ProductionOptions();
    }

    public decimal CalculateProfileCutLength(decimal clearSize, bool isFrameOrSash)
    {
        ValidatePositive(clearSize, nameof(clearSize));
        return clearSize + _options.WeldingAllowance * 2;
    }

    public decimal CalculateImpostLength(decimal innerOpeningSize)
    {
        ValidatePositive(innerOpeningSize, nameof(innerOpeningSize));
        return innerOpeningSize + _options.ImpostMillingAllowance * 2;
    }

    public GlassDimensions CalculateGlassDimensions(decimal opWidth, decimal opHeight, bool isInsideSash)
    {
        ValidatePositive(opWidth, nameof(opWidth));
        ValidatePositive(opHeight, nameof(opHeight));

        var sashCorrection = isInsideSash ? _options.SashOverlap * 2 : 0m;
        var width = opWidth + sashCorrection - _options.GlassGlazingGap * 2;
        var height = opHeight + sashCorrection - _options.GlassGlazingGap * 2;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("Calculated glass dimensions must be greater than zero.");
        }

        var area = decimal.Round(width * height / 1_000_000m, 4, MidpointRounding.AwayFromZero);
        return new GlassDimensions(width, height, area);
    }

    public HardwareKit CalculateHardwareKit(decimal w, decimal h)
    {
        ValidatePositive(w, nameof(w));
        ValidatePositive(h, nameof(h));

        var hinges = h switch
        {
            <= 900m => 2,
            <= 1600m => 3,
            _ => 4
        };

        var strikerPlates = h switch
        {
            <= 900m => 2,
            <= 1600m => 3,
            _ => 4
        };

        var sizeGroup = h switch
        {
            <= 900m => "Small sash",
            <= 1600m => "Standard sash",
            _ => "Tall sash"
        };

        var shortScrews = hinges * 4 + strikerPlates * 2;
        var longScrews = hinges * 2 + (w > 900m ? 4 : 2);

        return new HardwareKit(
            Hinges: hinges,
            Handles: 1,
            ShortScrews: shortScrews,
            LongScrews: longScrews,
            StrikerPlates: strikerPlates,
            SizeGroup: sizeGroup);
    }

    private static void ValidatePositive(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
        }
    }
}
