namespace NurSapat.Core.Models.Currency;

/// <summary>Исторический курс валюты НБКР: сколько KGS за одну единицу <see cref="CurrencyCode"/>.</summary>
public sealed class CurrencyRate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    /// <summary>Дата, за которую Нацбанк опубликовал курс.</summary>
    public DateTime RateDate { get; set; }

    /// <summary>Момент фактического скачивания и сохранения в БД.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
