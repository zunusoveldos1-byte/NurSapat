namespace NurSapat.App.Models.Stock;

public sealed class GlassCutItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public static GlassCutItem CreateDefault() => new()
    {
        Name = "Новый обрезок стекла",
        WidthMm = 800,
        HeightMm = 1200
    };

    public GlassCutItem Clone() => new()
    {
        Id = Id,
        Article = Article,
        Name = Name,
        Color = Color,
        WidthMm = WidthMm,
        HeightMm = HeightMm
    };
}
