namespace NurSapat.App.Models.EditDialogs;

public sealed class OrderColorParameters
{
    public string ColorName { get; set; } = "Белый";

    public string ColorHex { get; set; } = "#FFFFFF";

    public bool ApplyToAllConstructions { get; set; } = true;

    public bool ApplyToAllSashes { get; set; } = true;

    public bool ApplyToAllImposts { get; set; } = true;

    public bool ApplyToAllDecorativeElements { get; set; } = true;
}
