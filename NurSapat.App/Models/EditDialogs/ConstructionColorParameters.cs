namespace NurSapat.App.Models.EditDialogs;

public sealed class ConstructionColorParameters
{
    public string ColorName { get; set; } = "Белый";

    public string ColorHex { get; set; } = "#FFFFFF";

    public ConstructionColorTargets Targets { get; set; } = ConstructionColorTargets.All;
}
