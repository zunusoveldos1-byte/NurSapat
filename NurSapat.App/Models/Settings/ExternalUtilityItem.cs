namespace NurSapat.App.Models.Settings;

public sealed class ExternalUtilityItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Number { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public ExternalUtilityItem Clone() => new()
    {
        Id = Id,
        Number = Number,
        Name = Name,
        FilePath = FilePath
    };
}
