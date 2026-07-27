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

    public OrderStatus Status { get; set; } = OrderStatus.Calculated;

    public string? FilePath { get; set; }

    public OrderHeaderItem Clone() => new()
    {
        Id = Id,
        OrderNumber = OrderNumber,
        DateCreated = DateCreated,
        CustomerName = CustomerName,
        StructuresCount = StructuresCount,
        TotalSum = TotalSum,
        Status = Status,
        FilePath = FilePath
    };
}
