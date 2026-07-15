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
    private readonly List<OrderTemplateItem> _items = [];

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

    private static List<OrderTemplateItem> CreateDemoTemplates() => [];
}
