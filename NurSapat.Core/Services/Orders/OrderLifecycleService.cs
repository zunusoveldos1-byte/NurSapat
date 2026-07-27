using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Services.Orders;

public sealed class OrderLifecycleService
{
    public void AssertCanEdit(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        AssertCanEdit(order.Status);
    }

    public void AssertCanEdit(OrderStatus status)
    {
        if (status >= OrderStatus.InProduction)
        {
            throw new InvalidOperationException(
                $"Заказ в статусе «{status.ToDisplayName()}» заблокирован для редактирования.");
        }
    }

    public void AssertCanTransition(Order order, OrderStatus targetStatus, bool hasCalculationSnapshot)
    {
        ArgumentNullException.ThrowIfNull(order);

        var current = order.Status;
        if (current == targetStatus)
        {
            return;
        }

        if (targetStatus == OrderStatus.Cancelled)
        {
            if (current == OrderStatus.Shipped)
            {
                throw new InvalidOperationException("Отгруженный заказ нельзя отменить.");
            }

            return;
        }

        var expected = current switch
        {
            OrderStatus.Draft => OrderStatus.Calculated,
            OrderStatus.Calculated => OrderStatus.Approved,
            OrderStatus.Approved => OrderStatus.InProduction,
            OrderStatus.InProduction => OrderStatus.CuttingCompleted,
            OrderStatus.CuttingCompleted => OrderStatus.Assembled,
            OrderStatus.Assembled => OrderStatus.ReadyForShipment,
            OrderStatus.ReadyForShipment => OrderStatus.Shipped,
            _ => (OrderStatus?)null
        };

        if (expected != targetStatus)
        {
            throw new InvalidOperationException(
                $"Недопустимый переход заказа: {current.ToDisplayName()} → {targetStatus.ToDisplayName()}.");
        }

        if (targetStatus == OrderStatus.InProduction && !hasCalculationSnapshot)
        {
            throw new InvalidOperationException(
                "Перед запуском заказа в производство необходимо зафиксировать расчетный Snapshot.");
        }
    }

    public void Transition(Order order, OrderStatus targetStatus, bool hasCalculationSnapshot)
    {
        AssertCanTransition(order, targetStatus, hasCalculationSnapshot);
        order.Status = targetStatus;
    }
}
