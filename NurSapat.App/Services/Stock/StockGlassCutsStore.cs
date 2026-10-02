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
    private readonly List<GlassCutItem> _items =
    [
        new()
        {
            Article = "GL-4-16-4",
            Name = "Стеклопакет 4-16-4",
            Color = "Прозрачный",
            WidthMm = 1200,
            HeightMm = 800
        },
        new()
        {
            Article = "GL-4-16-4",
            Name = "Стеклопакет 4-16-4",
            Color = "Прозрачный",
            WidthMm = 980,
            HeightMm = 640
        },
        new()
        {
            Article = "GL-4MM-CLR",
            Name = "Стекло 4 мм",
            Color = "Прозрачный",
            WidthMm = 1500,
            HeightMm = 900
        },
        new()
        {
            Article = "GL-4MM-GRY",
            Name = "Стекло 4 мм тонированное",
            Color = "Серый",
            WidthMm = 720,
            HeightMm = 1180
        },
        new()
        {
            Article = "IGU-24-WHT",
            Name = "Стеклопакет двухкамерный",
            Color = "Белый",
            WidthMm = 860,
            HeightMm = 1420
        }
    ];

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
