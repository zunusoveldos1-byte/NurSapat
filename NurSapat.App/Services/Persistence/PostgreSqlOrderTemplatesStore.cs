using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlOrderTemplatesStore : IOrderTemplatesStore
{
    private readonly IOrderTemplateRepository _repository;

    public PostgreSqlOrderTemplatesStore(IOrderTemplateRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<OrderTemplateItem> GetAll() =>
        PersistenceSync.Run(() => _repository.GetAllAsync())
            .Select(ToItem)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public OrderTemplateItem? GetById(Guid id)
    {
        var template = PersistenceSync.Run(() => _repository.GetByIdAsync(id));
        return template is null ? null : ToItem(template);
    }

    public void Add(OrderTemplateItem item) =>
        PersistenceSync.Run(() => _repository.SaveAsync(ToDomain(item)));

    public void Update(OrderTemplateItem item) =>
        PersistenceSync.Run(() => _repository.UpdateAsync(ToDomain(item)));

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteAsync(id));

    private static OrderTemplateItem ToItem(OrderTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        ManufacturerId = template.ManufacturerId,
        WindPressure = template.WindPressure,
        ProfitMargin = template.ProfitMargin,
        AdditionalExpensesPercent = template.AdditionalExpensesPercent,
        AdditionalExpensesFixed = template.AdditionalExpensesFixed,
        DismantlingPrice = template.DismantlingPrice,
        InstallationPrice = template.InstallationPrice,
        CurrencyCode = template.CurrencyCode,
        DefaultOrderNote = template.DefaultOrderNote,
        DefaultProductionDays = template.DefaultProductionDays,
        DefaultPrepaymentPercent = template.DefaultPrepaymentPercent,
        ApplyDealerDiscount = template.ApplyDealerDiscount,
        DealerDiscountPercent = template.DealerDiscountPercent
    };

    private static OrderTemplate ToDomain(OrderTemplateItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        ManufacturerId = item.ManufacturerId,
        WindPressure = item.WindPressure,
        ProfitMargin = item.ProfitMargin,
        AdditionalExpensesPercent = item.AdditionalExpensesPercent,
        AdditionalExpensesFixed = item.AdditionalExpensesFixed,
        DismantlingPrice = item.DismantlingPrice,
        InstallationPrice = item.InstallationPrice,
        CurrencyCode = item.CurrencyCode,
        DefaultOrderNote = item.DefaultOrderNote,
        DefaultProductionDays = item.DefaultProductionDays,
        DefaultPrepaymentPercent = item.DefaultPrepaymentPercent,
        ApplyDealerDiscount = item.ApplyDealerDiscount,
        DealerDiscountPercent = item.DealerDiscountPercent
    };
}
