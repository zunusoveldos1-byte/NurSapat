namespace NurSapat.App.Models.Config;

public sealed class HardwareAutoPickRule
{
    public double MinWidthFalzMm { get; set; }

    public double MaxWidthFalzMm { get; set; }

    public double MinHeightFalzMm { get; set; }

    public double MaxHeightFalzMm { get; set; }

    public int ScissorsCount { get; set; } = 1;

    public int MiddleLockCount { get; set; }

    public HardwareAutoPickRule Clone() => new()
    {
        MinWidthFalzMm = MinWidthFalzMm,
        MaxWidthFalzMm = MaxWidthFalzMm,
        MinHeightFalzMm = MinHeightFalzMm,
        MaxHeightFalzMm = MaxHeightFalzMm,
        ScissorsCount = ScissorsCount,
        MiddleLockCount = MiddleLockCount
    };
}
