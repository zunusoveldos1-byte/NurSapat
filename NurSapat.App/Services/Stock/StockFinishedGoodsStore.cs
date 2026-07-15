using NurSapat.App.Models.Orders;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Orders;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Stock;

public interface IStockFinishedGoodsStore
{
    IReadOnlyList<FinishedGoodItem> GetAll();

    FinishedGoodItem? GetById(Guid id);

    void Add(FinishedGoodItem item);

    void Update(FinishedGoodItem item);

    void Delete(Guid id);
}

public sealed class StockFinishedGoodsStore(IOrdersStore ordersStore) : IStockFinishedGoodsStore
{
    private readonly List<FinishedGoodItem> _items = [];

    public IReadOnlyList<FinishedGoodItem> GetAll() =>
        _items
            .OrderByDescending(item => item.DateIncoming)
            .ThenBy(item => item.OrderNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public FinishedGoodItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public void Add(FinishedGoodItem item)
    {
        PrepareItem(item);
        _items.Add(item);
    }

    public void Update(FinishedGoodItem item)
    {
        PrepareItem(item);
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);

    private void PrepareItem(FinishedGoodItem item)
    {
        item.LinkToOrder(ordersStore.GetAll());
    }

    private static List<FinishedGoodItem> CreateDemoItems() => [];
}
