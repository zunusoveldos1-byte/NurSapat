namespace NurSapat.App.Models.Materials;

public sealed class MaterialColorPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ColorArticle { get; set; } = string.Empty;

    public string ColorName { get; set; } = "Белый";

    public string ColorHex { get; set; } = "#FFFFFF";

    public decimal Price { get; set; }

    public string LinkArticle { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool UseCustomCutLength { get; set; }

    public decimal? CustomBarLengthMm { get; set; }
}
