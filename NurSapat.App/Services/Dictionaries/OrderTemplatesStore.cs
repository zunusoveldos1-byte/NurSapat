using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IOrderTemplatesStore
{
    IReadOnlyList<OrderTemplateItem> GetAll();

    OrderTemplateItem? GetById(Guid id);

    void Add(OrderTemplateItem item);

    void Update(OrderTemplateItem item);

    void Delete(Guid id);
}

public sealed class OrderTemplatesStore : IOrderTemplatesStore
{
    private readonly List<OrderTemplateItem> _items = CreateDemoTemplates();

    public IReadOnlyList<OrderTemplateItem> GetAll() =>
        _items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public OrderTemplateItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public void Add(OrderTemplateItem item) => _items.Add(item);

    public void Update(OrderTemplateItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);

    private static List<OrderTemplateItem> CreateDemoTemplates() =>
    [
        new()
        {
            Name = "Дилерский Ош",
            ManufacturerId = DictionarySeedIds.GalwinId,
            WindPressure = 420,
            ProfitMargin = 18m,
            AdditionalExpensesPercent = 5,
            AdditionalExpensesFixed = 1500m,
            DismantlingPrice = 800m,
            InstallationPrice = 2200m,
            CurrencyCode = "KGS"
        },
        new()
        {
            Name = "Розничный стандарт",
            ManufacturerId = null,
            WindPressure = 380,
            ProfitMargin = 25m,
            AdditionalExpensesPercent = 7,
            AdditionalExpensesFixed = 2000m,
            DismantlingPrice = 1000m,
            InstallationPrice = 3500m,
            CurrencyCode = "KGS"
        },
        new()
        {
            Name = "Опт",
            ManufacturerId = DictionarySeedIds.RehauId,
            WindPressure = 450,
            ProfitMargin = 12m,
            AdditionalExpensesPercent = 3,
            AdditionalExpensesFixed = 900m,
            DismantlingPrice = 600m,
            InstallationPrice = 1800m,
            CurrencyCode = "USD"
        }
    ];
}
