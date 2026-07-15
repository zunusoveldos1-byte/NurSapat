using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IManufacturersStore
{
    IReadOnlyList<ManufacturerItem> GetAll();

    ManufacturerItem? GetById(Guid id);

    void Add(ManufacturerItem item);

    void Update(ManufacturerItem item);

    void Delete(Guid id);
}

public sealed class ManufacturersStore : IManufacturersStore
{
    private readonly List<ManufacturerItem> _items = [];

    public IReadOnlyList<ManufacturerItem> GetAll() =>
        _items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public ManufacturerItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public void Update(ManufacturerItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Add(ManufacturerItem item) => _items.Add(item);

    public void Delete(Guid id) => _items.RemoveAll(item => item.Id == id);

    private static List<ManufacturerItem> CreateFixedManufacturers() => [];
}
