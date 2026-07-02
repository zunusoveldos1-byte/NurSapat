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
    private readonly List<SupplierItem> _items = CreateDemoSuppliers();

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

    private static List<SupplierItem> CreateDemoSuppliers() =>
    [
        new()
        {
            Name = "Комплект-Юг Ош",
            Phone = "+996 555 210 334",
            LegalAddress = "г. Ош, ул. Ленина 45",
            ContractTerms = "Поставка профиля, отсрочка 14 дней",
            Note = "Основной поставщик по югу"
        },
        new()
        {
            Name = "Профиль-Сервис Бишкек",
            Phone = "+996 312 98 76 54",
            LegalAddress = "г. Бишкек, пр. Чуй 120",
            ContractTerms = "Договор №ПС-2025/18, самовывоз",
            Note = string.Empty
        },
        new()
        {
            Name = "СтеклоТорг",
            Phone = "+996 700 445 667",
            LegalAddress = "г. Бишкек, ул. Токтогула 18",
            ContractTerms = "Стеклопакеты, предоплата 30%",
            Note = "Доставка по городу включена"
        }
    ];
}
