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
    private readonly List<FinishedGoodItem> _items = CreateDemoItems();

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

    private static List<FinishedGoodItem> CreateDemoItems() =>
    [
        new()
        {
            OrderId = DemoEntityIds.Order1024,
            OrderNumber = "#1024",
            ProductCode = "FG-OK-0142-01",
            CellAddress = "A-12",
            State = FinishedGoodState.OnWarehouse,
            DateIncoming = new DateTime(2026, 6, 28),
            UserName = "admin"
        },
        new()
        {
            OrderId = DemoEntityIds.Order1023,
            OrderNumber = "#1023",
            ProductCode = "FG-DV-0098-02",
            CellAddress = "B-03",
            State = FinishedGoodState.QualityControl,
            DateIncoming = new DateTime(2026, 6, 30),
            UserName = "manager"
        },
        new()
        {
            OrderId = DemoEntityIds.Order1022,
            OrderNumber = "#1022",
            ProductCode = "FG-BL-0061-01",
            CellAddress = "C-07",
            State = FinishedGoodState.Shipped,
            DateIncoming = new DateTime(2026, 6, 15),
            UserName = "admin"
        }
    ];
}
