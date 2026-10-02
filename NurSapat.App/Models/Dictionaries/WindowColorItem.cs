namespace NurSapat.App.Models.Dictionaries;

public sealed class WindowColorItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string HexCode { get; set; } = "#FFFFFF";
}
