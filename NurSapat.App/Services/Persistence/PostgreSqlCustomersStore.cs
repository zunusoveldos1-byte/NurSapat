using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlCustomersStore : ICustomersStore
{
    private readonly ICustomerRepository _repository;

    public PostgreSqlCustomersStore(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<CustomerItem> GetAll() =>
        _repository.GetAllAsync().GetAwaiter().GetResult()
            .Where(customer => !customer.IsDeleted)
            .Select(ToItem)
            .OrderBy(item => item.FullName)
            .ToList();

    public IReadOnlyList<CustomerItem> GetSelectable() =>
        GetAll().Where(item => item.CanSelectForOrder).ToList();

    public CustomerItem? GetById(Guid id)
    {
        var customer = _repository.GetByIdAsync(id).GetAwaiter().GetResult();
        return customer is null ? null : ToItem(customer);
    }

    public void Add(CustomerItem item)
    {
        item.ModifiedAt = DateTime.Now;
        if (item.CreatedAt == default)
        {
            item.CreatedAt = DateTime.Now;
        }

        _repository.SaveAsync(item).GetAwaiter().GetResult();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Update(CustomerItem item)
    {
        item.ModifiedAt = DateTime.Now;
        _repository.UpdateAsync(item).GetAwaiter().GetResult();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Delete(Guid id)
    {
        var existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        existing.IsDeleted = true;
        existing.ModifiedAt = DateTime.Now;
        _repository.UpdateAsync(existing).GetAwaiter().GetResult();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RecalculateFinancials(IEnumerable<(Guid CustomerId, decimal OrderTotal, decimal PaidAmount)> orderLines)
    {
        var aggregates = orderLines
            .GroupBy(line => line.CustomerId)
            .ToDictionary(
                group => group.Key,
                group => (
                    Turnover: group.Sum(x => x.PaidAmount),
                    Debt: Math.Max(0, group.Sum(x => x.OrderTotal) - group.Sum(x => x.PaidAmount))));

        var changed = false;
        foreach (var item in GetAll())
        {
            if (aggregates.TryGetValue(item.Id, out var values))
            {
                if (item.Turnover == values.Turnover && item.Debt == values.Debt)
                {
                    continue;
                }

                item.Turnover = values.Turnover;
                item.Debt = values.Debt;
                item.ModifiedAt = DateTime.Now;
                _repository.UpdateAsync(item).GetAwaiter().GetResult();
                changed = true;
            }
            else if (item.Turnover != 0 || item.Debt != 0)
            {
                item.Turnover = 0;
                item.Debt = 0;
                item.ModifiedAt = DateTime.Now;
                _repository.UpdateAsync(item).GetAwaiter().GetResult();
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static CustomerItem ToItem(Customer customer) => new()
    {
        Id = customer.Id,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Type = customer.Type,
        Turnover = customer.Turnover,
        Debt = customer.Debt,
        OkpoCode = customer.OkpoCode,
        BankName = customer.BankName,
        CheckingAccount = customer.CheckingAccount,
        Mfo = customer.Mfo,
        LegalAddress = customer.LegalAddress,
        City = customer.City,
        ActualAddress = customer.ActualAddress,
        DealerCode = customer.DealerCode,
        Note = customer.Note,
        IsBlocked = customer.IsBlocked,
        IsDeleted = customer.IsDeleted,
        CreatedAt = customer.CreatedAt,
        ModifiedAt = customer.ModifiedAt
    };
}
