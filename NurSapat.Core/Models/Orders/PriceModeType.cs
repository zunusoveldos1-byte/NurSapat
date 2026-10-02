namespace NurSapat.Core.Models.Orders;

public enum PriceModeType
{
    Retail,
    Dealer,
    Wholesale,
    CostPlus
}

public static class PriceModeTypeExtensions
{
    public static string ToDisplayName(this PriceModeType mode) => mode switch
    {
        PriceModeType.Retail => "Розничная цена",
        PriceModeType.Dealer => "Дилерская цена",
        PriceModeType.Wholesale => "Тендер / опт",
        PriceModeType.CostPlus => "Себестоимость + %",
        _ => mode.ToString()
    };

    public static IReadOnlyList<PriceModeType> All { get; } =
    [
        PriceModeType.Retail,
        PriceModeType.Dealer,
        PriceModeType.Wholesale,
        PriceModeType.CostPlus
    ];
}
