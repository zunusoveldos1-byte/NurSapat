namespace NurSapat.Core.Calculations;

public sealed record HardwareKit(
    int Hinges,
    int Handles,
    int ShortScrews,
    int LongScrews,
    int StrikerPlates,
    string SizeGroup);
