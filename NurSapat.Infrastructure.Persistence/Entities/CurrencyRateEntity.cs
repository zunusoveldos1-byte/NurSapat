namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class CurrencyRateEntity
{
    public Guid Id { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public DateTime RateDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
