using NurSapat.App.Models.Orders;
using NurSapat.Core.Validation;

namespace NurSapat.App.Models.Stock;

public sealed class FinishedGoodItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Идентификатор заказа (каноническая связь).</summary>
    public Guid OrderId { get; set; }

    /// <summary>Номер заказа для отображения (денормализация).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string CellAddress { get; set; } = string.Empty;

    public FinishedGoodState State { get; set; } = FinishedGoodState.OnWarehouse;

    public DateTime DateIncoming { get; set; } = DateTime.Today;

    public string UserName { get; set; } = string.Empty;

    public void Validate()
    {
        DomainInvariants.RequireNonEmptyGuid(OrderId, nameof(OrderId));
        DomainInvariants.RequireNonEmpty(OrderNumber, nameof(OrderNumber));
        DomainInvariants.RequireNonEmpty(ProductCode, nameof(ProductCode));
        DomainInvariants.RequireNonEmpty(CellAddress, nameof(CellAddress));
    }

    public void LinkToOrder(IEnumerable<OrderHeaderItem> orders)
    {
        if (OrderId != Guid.Empty)
        {
            return;
        }

        var match = orders.FirstOrDefault(order =>
            string.Equals(order.OrderNumber, OrderNumber, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            OrderId = match.Id;
        }
    }

    public FinishedGoodItem Clone() => new()
    {
        Id = Id,
        OrderId = OrderId,
        OrderNumber = OrderNumber,
        ProductCode = ProductCode,
        CellAddress = CellAddress,
        State = State,
        DateIncoming = DateIncoming,
        UserName = UserName
    };
}
