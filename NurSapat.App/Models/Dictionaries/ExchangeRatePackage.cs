namespace NurSapat.App.Models.Dictionaries;

public sealed class ExchangeRatePackage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime StartDate { get; set; } = DateTime.Today;

    public decimal UsdRate { get; set; }

    public decimal EurRate { get; set; }

    public decimal CnyRate { get; set; }

    public decimal UeRate { get; set; }

    public ExchangeRatePackage Clone() => new()
    {
        Id = Id,
        StartDate = StartDate,
        UsdRate = UsdRate,
        EurRate = EurRate,
        CnyRate = CnyRate,
        UeRate = UeRate
    };
}
