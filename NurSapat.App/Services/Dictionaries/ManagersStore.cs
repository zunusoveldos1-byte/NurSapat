using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IManagersStore
{
    event EventHandler? Changed;

    IReadOnlyList<ManagerItem> GetAll();

    IReadOnlyList<ManagerItem> GetSelectable();

    ManagerItem? GetById(Guid id);

    void Add(ManagerItem item);

    void Update(ManagerItem item);

    void Delete(Guid id);
}

public sealed class ManagersStore : IManagersStore
{
    private readonly List<ManagerItem> _items = [];

    public event EventHandler? Changed;

    public IReadOnlyList<ManagerItem> GetAll() =>
        _items.Where(x => !x.IsDeleted).OrderBy(x => x.FullName).ToList();

    public IReadOnlyList<ManagerItem> GetSelectable() =>
        _items.Where(x => x.CanSelectForOrder).OrderBy(x => x.FullName).ToList();

    public ManagerItem? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);

    public void Add(ManagerItem item)
    {
        item.ModifiedAt = DateTime.Now;
        if (item.CreatedAt == default)
        {
            item.CreatedAt = DateTime.Now;
        }

        _items.Add(item);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Update(ManagerItem item)
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
        item.IsActive = false;
        item.ModifiedAt = DateTime.Now;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
