using NurSapat.App.Models.Structures;

namespace NurSapat.App.Services.Structures;

public interface IStandardStructuresStore
{
    IReadOnlyList<StandardStructureGroup> GetGroups();

    IReadOnlyList<StandardStructureItem> GetStructures();

    IReadOnlyList<StandardStructureItem> GetStructuresByGroup(Guid groupId);

    StandardStructureGroup? GetGroupById(Guid id);

    StandardStructureItem? GetStructureById(Guid id);

    void AddGroup(StandardStructureGroup group);

    void UpdateGroup(StandardStructureGroup group);

    void DeleteGroup(Guid groupId);

    void AddStructure(StandardStructureItem structure);

    void UpdateStructure(StandardStructureItem structure);

    void DeleteStructure(Guid structureId);

    void DeleteAllStructures();
}

public sealed class StandardStructuresStore : IStandardStructuresStore
{
    private readonly List<StandardStructureGroup> _groups;
    private readonly List<StandardStructureItem> _structures;

    public StandardStructuresStore()
    {
        var doubleLeaf = new StandardStructureGroup { Name = "Двухстворчатые" };
        var arch = new StandardStructureGroup { Name = "Арочные" };
        var single = new StandardStructureGroup { Name = "Одностворчатые" };

        _groups = [doubleLeaf, arch, single];
        _structures =
        [
            new()
            {
                Name = "Окно 1200×1400",
                GroupId = doubleLeaf.Id,
                GeometryJson = """{"version":1,"type":"double-sash","width":1200,"height":1400}"""
            },
            new()
            {
                Name = "Окно 1500×1500 с импостом",
                GroupId = doubleLeaf.Id,
                GeometryJson = """{"version":1,"type":"double-sash-impost","width":1500,"height":1500}"""
            },
            new()
            {
                Name = "Арочное окно 900×1600",
                GroupId = arch.Id,
                GeometryJson = """{"version":1,"type":"arch","width":900,"height":1600}"""
            },
            new()
            {
                Name = "Балконная дверь 700×2100",
                GroupId = single.Id,
                GeometryJson = """{"version":1,"type":"door","width":700,"height":2100}"""
            }
        ];
    }

    public IReadOnlyList<StandardStructureGroup> GetGroups() =>
        _groups.OrderBy(group => group.Name).ToList();

    public IReadOnlyList<StandardStructureItem> GetStructures() =>
        _structures.OrderBy(item => item.Name).ToList();

    public IReadOnlyList<StandardStructureItem> GetStructuresByGroup(Guid groupId) =>
        _structures.Where(item => item.GroupId == groupId).OrderBy(item => item.Name).ToList();

    public StandardStructureGroup? GetGroupById(Guid id) =>
        _groups.FirstOrDefault(group => group.Id == id);

    public StandardStructureItem? GetStructureById(Guid id) =>
        _structures.FirstOrDefault(item => item.Id == id);

    public void AddGroup(StandardStructureGroup group) => _groups.Add(group);

    public void UpdateGroup(StandardStructureGroup group)
    {
        var index = _groups.FindIndex(item => item.Id == group.Id);
        if (index >= 0)
        {
            _groups[index] = group;
        }
    }

    public void DeleteGroup(Guid groupId)
    {
        _groups.RemoveAll(group => group.Id == groupId);
        _structures.RemoveAll(item => item.GroupId == groupId);
    }

    public void AddStructure(StandardStructureItem structure) => _structures.Add(structure);

    public void UpdateStructure(StandardStructureItem structure)
    {
        var index = _structures.FindIndex(item => item.Id == structure.Id);
        if (index >= 0)
        {
            var existing = _structures[index];
            _structures[index] = new StandardStructureItem
            {
                Id = existing.Id,
                Name = structure.Name,
                GroupId = structure.GroupId,
                ThumbnailPath = existing.ThumbnailPath,
                GeometryJson = existing.GeometryJson
            };
        }
    }

    public void DeleteStructure(Guid structureId) =>
        _structures.RemoveAll(item => item.Id == structureId);

    public void DeleteAllStructures() => _structures.Clear();
}
