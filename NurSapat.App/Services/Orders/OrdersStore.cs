using NurSapat.App.Models.Orders;

namespace NurSapat.App.Services.Orders;

public interface IOrdersStore
{
    IReadOnlyList<OrderHeaderItem> GetAll();

    Task RefreshAsync();
}

public sealed class OrdersStore : IOrdersStore
{
    private readonly List<OrderHeaderItem> _items = CreateDemoItems();

    public IReadOnlyList<OrderHeaderItem> GetAll() => _items;

    public async Task RefreshAsync()
    {
        await Task.Delay(350);

        foreach (var item in _items)
        {
            if (item.Status == OrderStatus.InProduction && Random.Shared.NextDouble() > 0.65)
            {
                item.Status = OrderStatus.Ready;
            }
        }
    }

    private static List<OrderHeaderItem> CreateDemoItems() =>
    [
        new()
        {
            OrderNumber = "#1024",
            DateCreated = new DateTime(2026, 6, 28),
            CustomerName = "Иванов Алексей Петрович",
            StructuresCount = 4,
            TotalSum = 186_450m,
            Status = OrderStatus.InProduction
        },
        new()
        {
            OrderNumber = "#1023",
            DateCreated = new DateTime(2026, 6, 25),
            CustomerName = "ОсОО «Окна Бишкек»",
            StructuresCount = 12,
            TotalSum = 742_800m,
            Status = OrderStatus.Ready
        },
        new()
        {
            OrderNumber = "#1022",
            DateCreated = new DateTime(2026, 6, 22),
            CustomerName = "Дилер «СтройПлюс»",
            StructuresCount = 8,
            TotalSum = 512_300m,
            Status = OrderStatus.Shipped
        },
        new()
        {
            OrderNumber = "#1021",
            DateCreated = new DateTime(2026, 6, 20),
            CustomerName = "Петрова Мария Сергеевна",
            StructuresCount = 2,
            TotalSum = 94_600m,
            Status = OrderStatus.Measurement
        },
        new()
        {
            OrderNumber = "#1020",
            DateCreated = new DateTime(2026, 6, 18),
            CustomerName = "ИП «Алма-Арча»",
            StructuresCount = 6,
            TotalSum = 328_900m,
            Status = OrderStatus.Calculation
        },
        new()
        {
            OrderNumber = "#1019",
            DateCreated = new DateTime(2026, 6, 15),
            CustomerName = "ОсОО «ТеплоДом»",
            StructuresCount = 15,
            TotalSum = 1_045_200m,
            Status = OrderStatus.InProduction
        },
        new()
        {
            OrderNumber = "#1018",
            DateCreated = new DateTime(2026, 6, 12),
            CustomerName = "Садыков Нурбек Т.",
            StructuresCount = 3,
            TotalSum = 127_500m,
            Status = OrderStatus.Ready
        },
        new()
        {
            OrderNumber = "#1017",
            DateCreated = new DateTime(2026, 6, 10),
            CustomerName = "ОсОО «Витраж Плюс»",
            StructuresCount = 9,
            TotalSum = 468_750m,
            Status = OrderStatus.Shipped
        }
    ];
}
