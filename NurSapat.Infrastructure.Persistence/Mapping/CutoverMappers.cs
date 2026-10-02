using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Users;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Mapping;

internal static class UserAccountMapper
{
    public static UserAccount ToDomain(UserEntity entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        PasswordHash = entity.PasswordHash,
        GroupNumber = entity.GroupNumber,
        PermissionsJson = entity.PermissionsJson
    };

    public static UserEntity ToEntity(UserAccount domain) => new()
    {
        Id = domain.Id,
        Username = domain.Username,
        PasswordHash = domain.PasswordHash,
        GroupNumber = domain.GroupNumber,
        PermissionsJson = domain.PermissionsJson
    };
}

internal static class OrderTemplateMapper
{
    public static OrderTemplate ToDomain(OrderTemplateEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ManufacturerId = entity.ManufacturerId,
        WindPressure = entity.WindPressure,
        ProfitMargin = entity.ProfitMargin,
        AdditionalExpensesPercent = entity.AdditionalExpensesPercent,
        AdditionalExpensesFixed = entity.AdditionalExpensesFixed,
        DismantlingPrice = entity.DismantlingPrice,
        InstallationPrice = entity.InstallationPrice,
        CurrencyCode = entity.CurrencyCode,
        DefaultOrderNote = entity.DefaultOrderNote,
        DefaultProductionDays = entity.DefaultProductionDays,
        DefaultPrepaymentPercent = entity.DefaultPrepaymentPercent,
        ApplyDealerDiscount = entity.ApplyDealerDiscount,
        DealerDiscountPercent = entity.DealerDiscountPercent
    };

    public static OrderTemplateEntity ToEntity(OrderTemplate domain) => new()
    {
        Id = domain.Id,
        Name = domain.Name,
        ManufacturerId = domain.ManufacturerId,
        WindPressure = domain.WindPressure,
        ProfitMargin = domain.ProfitMargin,
        AdditionalExpensesPercent = domain.AdditionalExpensesPercent,
        AdditionalExpensesFixed = domain.AdditionalExpensesFixed,
        DismantlingPrice = domain.DismantlingPrice,
        InstallationPrice = domain.InstallationPrice,
        CurrencyCode = domain.CurrencyCode,
        DefaultOrderNote = domain.DefaultOrderNote,
        DefaultProductionDays = domain.DefaultProductionDays,
        DefaultPrepaymentPercent = domain.DefaultPrepaymentPercent,
        ApplyDealerDiscount = domain.ApplyDealerDiscount,
        DealerDiscountPercent = domain.DealerDiscountPercent
    };
}
