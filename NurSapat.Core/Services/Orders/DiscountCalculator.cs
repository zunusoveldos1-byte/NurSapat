namespace NurSapat.Core.Services.Orders;

public static class DiscountCalculator
{
    public static decimal PercentFromSum(decimal subtotal, decimal discountSum)
    {
        if (subtotal <= 0 || discountSum <= 0)
        {
            return 0;
        }

        return Math.Round(discountSum / subtotal * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal SumFromPercent(decimal subtotal, decimal discountPercent)
    {
        if (subtotal <= 0 || discountPercent <= 0)
        {
            return 0;
        }

        return Math.Round(subtotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateMargin(decimal retailPrice, decimal costPrice) =>
        Math.Round(retailPrice - costPrice, 2, MidpointRounding.AwayFromZero);
}
