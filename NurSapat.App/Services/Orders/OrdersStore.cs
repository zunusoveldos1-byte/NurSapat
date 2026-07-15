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
            var demo = CreateDemoItems();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(demo, JsonOptions));
            return demo;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<OrderHeaderItem>>(json, JsonOptions) ?? CreateDemoItems();
        }
        catch
        {
            return CreateDemoItems();
        }
    }

    private static List<OrderHeaderItem> CreateDemoItems() =>
    [
        new()
        {
            Id = DemoEntityIds.Order1024,
            OrderNumber = "#1024",
            DateCreated = new DateTime(2026, 6, 28),
            CustomerName = "Иванов Алексей Петрович",
            StructuresCount = 4,
            TotalSum = 186_450m,
            Status = OrderStatus.InProduction
        },
        new()
        {
            Id = DemoEntityIds.Order1023,
            OrderNumber = "#1023",
            DateCreated = new DateTime(2026, 6, 25),
            CustomerName = "ОсОО «Окна Бишкек»",
            StructuresCount = 12,
            TotalSum = 742_800m,
            Status = OrderStatus.Ready
        },
        new()
        {
            Id = DemoEntityIds.Order1022,
            OrderNumber = "#1022",
            DateCreated = new DateTime(2026, 6, 22),
            CustomerName = "Дилер «СтройПлюс»",
            StructuresCount = 8,
            TotalSum = 512_300m,
            Status = OrderStatus.Shipped
        },
        new()
        {
            Id = DemoEntityIds.Order1021,
            OrderNumber = "#1021",
            DateCreated = new DateTime(2026, 6, 20),
            CustomerName = "Петрова Мария Сергеевна",
            StructuresCount = 2,
            TotalSum = 94_600m,
            Status = OrderStatus.Calculation
        },
        new()
        {
            Id = DemoEntityIds.Order1020,
            OrderNumber = "#1020",
            DateCreated = new DateTime(2026, 6, 18),
            CustomerName = "ИП «Алма-Арча»",
            StructuresCount = 6,
            TotalSum = 328_900m,
            Status = OrderStatus.Calculation
        },
        new()
        {
            Id = DemoEntityIds.Order1019,
            OrderNumber = "#1019",
            DateCreated = new DateTime(2026, 6, 15),
            CustomerName = "ОсОО «ТеплоДом»",
            StructuresCount = 15,
            TotalSum = 1_045_200m,
            Status = OrderStatus.InProduction
        },
        new()
        {
            Id = DemoEntityIds.Order1018,
            OrderNumber = "#1018",
            DateCreated = new DateTime(2026, 6, 12),
            CustomerName = "Садыков Нурбек Т.",
            StructuresCount = 3,
            TotalSum = 127_500m,
            Status = OrderStatus.Ready
        },
        new()
        {
            Id = DemoEntityIds.Order1017,
            OrderNumber = "#1017",
            DateCreated = new DateTime(2026, 6, 10),
            CustomerName = "ОсОО «Витраж Плюс»",
            StructuresCount = 9,
            TotalSum = 468_750m,
            Status = OrderStatus.Shipped
        }
    ];

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
