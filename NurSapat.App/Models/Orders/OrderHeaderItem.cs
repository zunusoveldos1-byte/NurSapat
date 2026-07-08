using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Models.Orders;

public sealed class OrderHeaderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime DateCreated { get; set; } = DateTime.Today;

    public string CustomerName { get; set; } = string.Empty;

    public int StructuresCount { get; set; }

    public decimal TotalSum { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Calculation;
}
