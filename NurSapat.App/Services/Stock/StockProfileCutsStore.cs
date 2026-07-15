using NurSapat.App.Models.Stock;

namespace NurSapat.App.Services.Stock;

public interface IStockProfileCutsStore
{
    IReadOnlyList<ProfileCutItem> GetAll();

    ProfileCutItem? GetById(Guid id);

    void Add(ProfileCutItem item);

    void Update(ProfileCutItem item);

    void Delete(Guid id);
}

public sealed class StockProfileCutsStore : IStockProfileCutsStore
{
    private readonly List<ProfileCutItem> _items = [];

    public IReadOnlyList<ProfileCutItem> GetAll() => _items;

    public ProfileCutItem? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

    public void Add(ProfileCutItem item) => _items.Add(item);

    public void Update(ProfileCutItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);
}
