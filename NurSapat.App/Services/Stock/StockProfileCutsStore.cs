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
    private readonly List<ProfileCutItem> _items =
    [
        new()
        {
            Article = "RH-70-WHT",
            Name = "Рама Rehau 70",
            Color = "Белый",
            LengthMm = 1240
        },
        new()
        {
            Article = "RH-70-WHT",
            Name = "Импост Rehau 70",
            Color = "Белый",
            LengthMm = 860
        },
        new()
        {
            Article = "GALWIN-60-FR",
            Name = "Рама Galwin 60",
            Color = "Золотой дуб",
            LengthMm = 1580
        },
        new()
        {
            Article = "GALWIN-60-FR",
            Name = "Створка Galwin 60",
            Color = "Золотой дуб",
            LengthMm = 720
        },
        new()
        {
            Article = "ALU-50-ANT",
            Name = "Рама алюминий 50",
            Color = "Антрацит",
            LengthMm = 1800
        },
        new()
        {
            Article = "ALU-50-ANT",
            Name = "Импост алюминий 50",
            Color = "Антрацит",
            LengthMm = 600
        },
        new()
        {
            Article = "RH-70-WHT",
            Name = "Рама Rehau 70",
            Color = "Махагон",
            LengthMm = 1320
        }
    ];

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
