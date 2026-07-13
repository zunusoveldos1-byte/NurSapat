using NurSapat.App.Models.Dictionaries;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Dictionaries;

public interface ICustomersStore
{
    event EventHandler? Changed;

    /// <summary>Активные (не удалённые) заказчики.</summary>
    IReadOnlyList<CustomerItem> GetAll();

    /// <summary>Заказчики, доступные для выбора в заказе.</summary>
    IReadOnlyList<CustomerItem> GetSelectable();

    CustomerItem? GetById(Guid id);

    void Add(CustomerItem item);

    void Update(CustomerItem item);

    /// <summary>Мягкое удаление.</summary>
    void Delete(Guid id);

    /// <summary>
    /// Пересчёт оборота и долга по оплаченным/неоплаченным заказам.
    /// turnover = сумма оплат, debt = сумма заказов − сумма оплат.
    /// </summary>
    void RecalculateFinancials(IEnumerable<(Guid CustomerId, decimal OrderTotal, decimal PaidAmount)> orderLines);
}

public sealed class CustomersStore : ICustomersStore
{
    private readonly List<CustomerItem> _items = [];
    private int _rowNumberSeed;

    public event EventHandler? Changed;

    public IReadOnlyList<CustomerItem> GetAll() =>
        _items.Where(x => !x.IsDeleted).OrderBy(x => x.FullName).ToList();

    public IReadOnlyList<CustomerItem> GetSelectable() =>
        _items.Where(x => x.CanSelectForOrder).OrderBy(x => x.FullName).ToList();

    public CustomerItem? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);

    public void Add(CustomerItem item)
    {
        item.ModifiedAt = DateTime.Now;
        if (item.CreatedAt == default)
        {
            item.CreatedAt = DateTime.Now;
        }

        _items.Add(item);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Update(CustomerItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index < 0)
        {
            return;
        }

        item.ModifiedAt = DateTime.Now;
        _items[index] = item;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Delete(Guid id)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return;
        }

        item.IsDeleted = true;
        item.ModifiedAt = DateTime.Now;
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
        foreach (var item in _items)
        {
            if (aggregates.TryGetValue(item.Id, out var values))
            {
                if (item.Turnover != values.Turnover || item.Debt != values.Debt)
                {
                    item.Turnover = values.Turnover;
                    item.Debt = values.Debt;
                    item.ModifiedAt = DateTime.Now;
                    changed = true;
                }
            }
            else if (item.Turnover != 0 || item.Debt != 0)
            {
                item.Turnover = 0;
                item.Debt = 0;
                item.ModifiedAt = DateTime.Now;
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public int NextDisplayNumber() => ++_rowNumberSeed;
}
