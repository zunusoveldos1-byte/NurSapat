namespace NurSapat.Core.Models.Orders;

public sealed class ConstructionAppearance
{
    public string ColorName { get; set; } = "Белый";

    public string ColorHex { get; set; } = "#FFFFFF";

    public string ContourColorName { get; set; } = "Gray";

    public bool ChangeProfile { get; set; } = true;

    public bool ChangeImposts { get; set; } = true;

    public bool ChangeSashes { get; set; } = true;

    public bool ChangeLayout { get; set; } = true;

    public bool ChangeDecorativeElements { get; set; } = true;
}
