namespace NurSapat.App.Models.Structures;

public sealed class StandardStructureItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid GroupId { get; set; }

    public string ThumbnailPath { get; set; } = string.Empty;

    public string GeometryJson { get; set; } = """{"version":1,"nodes":[]}""";

    public StandardStructureItem Clone() => new()
    {
        Id = Id,
        Name = Name,
        GroupId = GroupId,
        ThumbnailPath = ThumbnailPath,
        GeometryJson = GeometryJson
    };
}
