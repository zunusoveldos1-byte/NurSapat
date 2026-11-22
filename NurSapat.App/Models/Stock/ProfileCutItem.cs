namespace NurSapat.App.Models.Stock;

public sealed class ProfileCutItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public double LengthMm { get; set; }

    public static ProfileCutItem CreateDefault() => new()
    {
        Name = "Новый обрезок",
        LengthMm = 1000
    };

    public ProfileCutItem Clone() => new()
    {
        Id = Id,
        Article = Article,
        Name = Name,
        Color = Color,
        LengthMm = LengthMm
    };
}
