namespace NurSapat.App.Models.Structures;

public sealed class StandardStructureGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
}
