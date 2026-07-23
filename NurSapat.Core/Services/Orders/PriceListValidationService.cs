namespace NurSapat.Core.Services.Orders;

public sealed class PriceListValidationResult
{
    public bool RequiresRecalculation { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// Проверка актуальности прайс-листов при изменении даты заказа.
/// </summary>
public static class PriceListValidationService
{
    public static PriceListValidationResult ValidateOrderDateChange(DateTime previousDate, DateTime newDate)
    {
        if (previousDate.Month == newDate.Month && previousDate.Year == newDate.Year)
        {
            return new PriceListValidationResult { RequiresRecalculation = false };
        }

        return new PriceListValidationResult
        {
            RequiresRecalculation = true,
            Message = $"Дата заказа изменена с {previousDate:dd.MM.yyyy} на {newDate:dd.MM.yyyy}. " +
                      "Прайс-листы профиля ПВХ будут пересчитаны по тарифам нового периода."
        };
    }
}
