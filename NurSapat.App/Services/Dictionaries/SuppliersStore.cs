using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface ISuppliersStore
{
    IReadOnlyList<SupplierItem> GetAll();

    SupplierItem? GetById(Guid id);

    void Add(SupplierItem item);

    void Update(SupplierItem item);

    void Delete(Guid id);
}

public sealed class SuppliersStore : ISuppliersStore
{
    private readonly List<SupplierItem> _items = [];

    public IReadOnlyList<SupplierItem> GetAll() =>
        _items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public SupplierItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public void Add(SupplierItem item) => _items.Add(item);

    public void Update(SupplierItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);

    private static List<SupplierItem> CreateDemoSuppliers() => [];
}
