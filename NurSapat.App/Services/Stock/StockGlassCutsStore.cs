using NurSapat.App.Models.Stock;

namespace NurSapat.App.Services.Stock;

public interface IStockGlassCutsStore
{
    IReadOnlyList<GlassCutItem> GetAll();

    GlassCutItem? GetById(Guid id);

    void Add(GlassCutItem item);

    void Update(GlassCutItem item);

    void Delete(Guid id);
}

public sealed class StockGlassCutsStore : IStockGlassCutsStore
{
    private readonly List<GlassCutItem> _items = [];

    public IReadOnlyList<GlassCutItem> GetAll() => _items;

    public GlassCutItem? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

    public void Add(GlassCutItem item) => _items.Add(item);

    public void Update(GlassCutItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);
}
