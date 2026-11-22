namespace NurSapat.Core.Models.Orders;

public enum OrderStatus
{
    Draft = 0,
    Calculated = 10,
    Approved = 20,
    InProduction = 30,
    CuttingCompleted = 40,
    Assembled = 50,
    ReadyForShipment = 60,
    Shipped = 70,
    Cancelled = 99
}

public static class OrderStatusExtensions
{
    public static string ToDisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Draft => "Черновик",
        OrderStatus.Calculated => "Рассчитан",
        OrderStatus.Approved => "Утверждён",
        OrderStatus.InProduction => "В производстве",
        OrderStatus.CuttingCompleted => "Раскрой завершён",
        OrderStatus.Assembled => "Собран",
        OrderStatus.ReadyForShipment => "Готов к отгрузке",
        OrderStatus.Shipped => "Отгружен",
        OrderStatus.Cancelled => "Отменён",
        _ => status.ToString()
    };

    public static IReadOnlyList<OrderStatus> All { get; } =
    [
        OrderStatus.Draft,
        OrderStatus.Calculated,
        OrderStatus.Approved,
        OrderStatus.InProduction,
        OrderStatus.CuttingCompleted,
        OrderStatus.Assembled,
        OrderStatus.ReadyForShipment,
        OrderStatus.Shipped,
        OrderStatus.Cancelled
    ];
}
