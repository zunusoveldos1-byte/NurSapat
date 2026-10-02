namespace NurSapat.App.Services.Dictionaries;

public interface IWindowColorsStore
{
    IReadOnlyList<Models.Dictionaries.WindowColorItem> GetAll();

    void Add(Models.Dictionaries.WindowColorItem item);

    void Update(Models.Dictionaries.WindowColorItem item);

    void Delete(Guid id);
}

public sealed class WindowColorsStore : IWindowColorsStore
{
    private readonly List<Models.Dictionaries.WindowColorItem> _items =
    [
        new() { Name = "Белый", HexCode = "#FFFFFF" },
        new() { Name = "Махагон", HexCode = "#4E2A1D" },
        new() { Name = "Золотой дуб", HexCode = "#9B6636" },
        new() { Name = "Антрацит", HexCode = "#2C3E50" }
    ];

    public IReadOnlyList<Models.Dictionaries.WindowColorItem> GetAll() => _items;

    public void Add(Models.Dictionaries.WindowColorItem item) => _items.Add(item);

    public void Update(Models.Dictionaries.WindowColorItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);
}
