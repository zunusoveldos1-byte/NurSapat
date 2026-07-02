namespace NurSapat.App.Services.Dictionaries;

public interface ICustomersStore
{
    IReadOnlyList<Models.Dictionaries.CustomerItem> GetAll();

    Models.Dictionaries.CustomerItem? GetById(Guid id);

    void Add(Models.Dictionaries.CustomerItem item);

    void Update(Models.Dictionaries.CustomerItem item);

    void Delete(Guid id);
}

public sealed class CustomersStore : ICustomersStore
{
    private readonly List<Models.Dictionaries.CustomerItem> _items =
    [
        new()
        {
            FullName = "Иванов Алексей Петрович",
            Phone = "+996 555 123 456",
            Type = Models.Dictionaries.CustomerType.Individual,
            Turnover = 485_600m,
            Debt = 0m,
            OkpoCode = "21456398741236",
            BankName = "ОАО «РСК Банк»",
            CheckingAccount = "11800000012345678901",
            Mfo = "118001",
            LegalAddress = "г. Бишкек, ул. Чуй 145, кв. 32",
            Note = "Постоянный розничный клиент"
        },
        new()
        {
            FullName = "ОсОО «Окна Бишкек»",
            Phone = "+996 312 45 67 89",
            Type = Models.Dictionaries.CustomerType.LegalEntity,
            Turnover = 2_150_000m,
            Debt = 125_400m,
            OkpoCode = "30567890123456",
            BankName = "ОАО «Кыргызкоммерцбанк»",
            CheckingAccount = "10900000098765432109",
            Mfo = "109001",
            LegalAddress = "г. Бишкек, пр. Манаса 40, офис 12",
            Note = "Оплата по договору, отсрочка 14 дней"
        },
        new()
        {
            FullName = "Дилер «СтройПлюс»",
            Phone = "+996 700 987 654",
            Type = Models.Dictionaries.CustomerType.Dealer,
            Turnover = 980_300m,
            Debt = 42_000m,
            OkpoCode = "30890123456789",
            BankName = "ОАО «Демир Банк»",
            CheckingAccount = "12900000011223344556",
            Mfo = "129001",
            LegalAddress = "г. Ош, ул. Ленина 88",
            Note = "Дилерская скидка 12%"
        }
    ];

    public IReadOnlyList<Models.Dictionaries.CustomerItem> GetAll() => _items;

    public Models.Dictionaries.CustomerItem? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);

    public void Add(Models.Dictionaries.CustomerItem item) => _items.Add(item);

    public void Update(Models.Dictionaries.CustomerItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);
}
