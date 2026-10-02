using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Services.Orders;

public static class OrderBusinessRules
{
    public const decimal MaxManagerRetailDiscountPercent = 15m;

    public static bool IsCadReadOnly(OrderStatus status) => status == OrderStatus.InProduction;

    public static decimal ClampManagerDiscountPercent(decimal percent) =>
        percent < 0 ? 0 : Math.Min(percent, MaxManagerRetailDiscountPercent);

    public static bool CanSendToProduction(Order order, Dealer? dealer, out string? blockReason)
    {
        blockReason = null;
        if (dealer is null)
        {
            return true;
        }

        var orderAmount = order.Totals.GrandTotal;
        if (!dealer.IsWithinCreditLimit(orderAmount))
        {
            blockReason =
                $"Превышен кредитный лимит дилера. Доступно: {dealer.AvailableCredit:N0} {order.Header.CurrencyCode}.";
            return false;
        }

        return true;
    }
}
