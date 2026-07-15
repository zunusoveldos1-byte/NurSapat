using NurSapat.App.Models.Orders;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Settings;
using System.Text.Json;

namespace NurSapat.App.Services.Orders;

public interface IOrdersStore
{
    IReadOnlyList<OrderHeaderItem> GetAll();

    OrderHeaderItem? GetById(Guid id);

    void Add(OrderHeaderItem item);

    void Update(OrderHeaderItem item);

    void Delete(Guid id);

    void UpsertFromProject(ProjectDocumentData project, string filePath);

    /// <summary>Loads a full project snapshot from the store when supported (PostgreSQL); otherwise null.</summary>
    ProjectDocumentData? TryLoadProject(Guid id);

    Task RefreshAsync();

    void Save();
}

public sealed class OrdersStore : IOrdersStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly List<OrderHeaderItem> _items;

    public OrdersStore(EnvironmentSettings environmentSettings)
    {
        _filePath = Path.Combine(environmentSettings.AppDataPath, "orders-registry.json");
        _items = LoadOrCreate();
    }

    public IReadOnlyList<OrderHeaderItem> GetAll() =>
        _items.OrderByDescending(item => item.DateCreated).ToList();

    public OrderHeaderItem? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

    public void Add(OrderHeaderItem item)
    {
        _items.Add(item);
        Save();
    }

    public void Update(OrderHeaderItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
            Save();
        }
    }

    public void Delete(Guid id)
    {
        _items.RemoveAll(item => item.Id == id);
        Save();
    }

    public void UpsertFromProject(ProjectDocumentData project, string filePath)
    {
        var header = project.Order.Header;
        var existing = _items.FirstOrDefault(item =>
            string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase)
            || item.Id == project.Order.Id);

        if (existing is null)
        {
            existing = new OrderHeaderItem { Id = project.Order.Id == Guid.Empty ? Guid.NewGuid() : project.Order.Id };
            _items.Add(existing);
        }

        existing.OrderNumber = string.IsNullOrWhiteSpace(header.OrderNumber) ? existing.OrderNumber : header.OrderNumber;
        existing.DateCreated = header.OrderDate == default ? existing.DateCreated : header.OrderDate;
        existing.CustomerName = header.CustomerName;
        existing.StructuresCount = project.Order.Constructions.Count;
        existing.TotalSum = ResolveTotalSum(project);
        existing.Status = header.Status;
        existing.FilePath = filePath;
        Save();
    }

    public ProjectDocumentData? TryLoadProject(Guid id) => null;

    public Task RefreshAsync()
    {
        var removed = 0;
        var updated = 0;

        foreach (var item in _items.ToList())
        {
            if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(item.FilePath);
                var project = JsonSerializer.Deserialize<ProjectDocumentData>(json, JsonOptions);
                if (project is null)
                {
                    continue;
                }

                item.OrderNumber = project.Order.Header.OrderNumber;
                item.DateCreated = project.Order.Header.OrderDate;
                item.CustomerName = project.Order.Header.CustomerName;
                item.StructuresCount = project.Order.Constructions.Count;
                item.TotalSum = ResolveTotalSum(project);
                item.Status = project.Order.Header.Status;
                updated++;
            }
            catch
            {
                removed++;
            }
        }

        Save();
        return Task.FromResult(updated + removed);
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(_items, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private List<OrderHeaderItem> LoadOrCreate()
    {
        if (!File.Exists(_filePath))
        {
            var empty = new List<OrderHeaderItem>();
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(empty, JsonOptions));
            return empty;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<OrderHeaderItem>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static decimal ResolveTotalSum(ProjectDocumentData project)
    {
        if (project.Order.Payment.AgreedAmount > 0)
        {
            return project.Order.Payment.AgreedAmount;
        }

        var totals = project.Order.Totals;
        return totals.ConstructionsSubtotal
               + totals.DeliveryCost
               + totals.AdditionalMaterialsCost
               + totals.AdditionalMaterialsInstallationCost;
    }
}
