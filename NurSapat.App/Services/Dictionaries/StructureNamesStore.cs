using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IStructureNamesStore
{
    IReadOnlyList<StructureNameItem> GetAll();

    StructureNameItem? GetById(Guid id);

    StructureNameItem? GetByCode(string code);

    void Add(StructureNameItem item);

    void Update(StructureNameItem item);

    void Delete(Guid id);
}

public sealed class StructureNamesStore : IStructureNamesStore
{
    private readonly List<StructureNameItem> _items = [];

    public IReadOnlyList<StructureNameItem> GetAll() =>
        _items.OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase).ToList();

    public StructureNameItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public StructureNameItem? GetByCode(string code) =>
        _items.FirstOrDefault(item =>
            item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public void Add(StructureNameItem item) => _items.Add(item);

    public void Update(StructureNameItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);

    private static List<StructureNameItem> CreateDemoItems() => [];
}
