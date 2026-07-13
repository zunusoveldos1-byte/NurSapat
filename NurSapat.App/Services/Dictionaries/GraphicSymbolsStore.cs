using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IGraphicSymbolsStore
{
    IReadOnlyList<GraphicSymbolItem> GetAll();

    GraphicSymbolItem? GetById(Guid id);

    void Add(GraphicSymbolItem item);

    void Update(GraphicSymbolItem item);

    void Delete(Guid id);
}

public sealed class GraphicSymbolsStore : IGraphicSymbolsStore
{
    private readonly List<GraphicSymbolItem> _items = CreateSeedItems();

    public IReadOnlyList<GraphicSymbolItem> GetAll() => _items;

    public GraphicSymbolItem? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

    public void Add(GraphicSymbolItem item) => _items.Add(item);

    public void Update(GraphicSymbolItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);

    private static List<GraphicSymbolItem> CreateSeedItems() =>
    [
        new() { Name = "Ручка справа", Code = "HND-R" },
        new() { Name = "Ручка слева", Code = "HND-L" },
        new() { Name = "Замок", Code = "LCK-01" },
        new() { Name = "Ограничитель", Code = "LIM-01" }
    ];
}
