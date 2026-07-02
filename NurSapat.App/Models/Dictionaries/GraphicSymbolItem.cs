namespace NurSapat.App.Models.Dictionaries;

public sealed class GraphicSymbolItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
