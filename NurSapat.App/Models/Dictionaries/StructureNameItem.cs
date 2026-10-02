namespace NurSapat.App.Models.Dictionaries;

public sealed class StructureNameItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public StructureNameItem Clone() => new()
    {
        Id = Id,
        Code = Code,
        Name = Name
    };
}
