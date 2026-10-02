namespace NurSapat.Core.Models.Orders;

public enum OrderStatus
{
    Draft,
    Calculation,
    InProduction,
    Ready,
    Shipped
}

public static class OrderStatusExtensions
{
    public static string ToDisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Draft => "Черновик",
        OrderStatus.Calculation => "Просчёт",
        OrderStatus.InProduction => "В производстве",
        OrderStatus.Ready => "Готов",
        OrderStatus.Shipped => "Отгружен",
        _ => status.ToString()
    };

    public static IReadOnlyList<OrderStatus> All { get; } =
    [
        OrderStatus.Draft,
        OrderStatus.Calculation,
        OrderStatus.InProduction,
        OrderStatus.Ready,
        OrderStatus.Shipped
    ];
}
