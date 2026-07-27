using NurSapat.Core.Models.Currency;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Mapping;

internal static class CurrencyRateMapper
{
    public static CurrencyRate ToDomain(CurrencyRateEntity entity) =>
        new()
        {
            Id = entity.Id,
            CurrencyCode = entity.CurrencyCode,
            Rate = entity.Rate,
            RateDate = entity.RateDate,
            CreatedAt = entity.CreatedAt
        };

    public static CurrencyRateEntity ToEntity(CurrencyRate rate) =>
        new()
        {
            Id = rate.Id,
            CurrencyCode = rate.CurrencyCode,
            Rate = rate.Rate,
            RateDate = rate.RateDate.Date,
            CreatedAt = rate.CreatedAt
        };
}
