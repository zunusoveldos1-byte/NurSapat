using NurSapat.App.Models.Orders;
using NurSapat.App.Services.Users;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.App.Services.Orders;

/// <summary>
/// Offline-first orders store: SQLite + isolation by current user_id.
/// </summary>
public sealed class SyncedOrdersStore : IOrdersStore
{
    private readonly IOfflineOrderRepository _repository;
    private readonly IUserSessionService _session;
    private readonly ISupabaseAuthService _auth;

    public SyncedOrdersStore(
        IOfflineOrderRepository repository,
        IUserSessionService session,
        ISupabaseAuthService auth)
    {
        _repository = repository;
        _session = session;
        _auth = auth;
    }

    private Guid? CurrentUserId =>
        _session.CurrentUser?.Id
        ?? _auth.CurrentUserId;

    public IReadOnlyList<OrderHeaderItem> GetAll()
    {
        var userId = CurrentUserId;
        return Run(() => _repository.GetAllAsync())
            .Where(row => userId is null || row.UserId == userId)
            .Select(ToItem)
            .OrderByDescending(item => item.DateCreated)
            .ToList();
    }

    public OrderHeaderItem? GetById(Guid id)
    {
        var row = Run(() => _repository.GetByIdAsync(id));
        if (row is null)
        {
            return null;
        }

        var userId = CurrentUserId;
        if (userId is not null && row.UserId != userId)
        {
            return null;
        }

        return ToItem(row);
    }

    public void Add(OrderHeaderItem item)
    {
        var userId = CurrentUserId
            ?? throw new InvalidOperationException("Нет текущего пользователя для сохранения заказа.");

        if (item.Id == Guid.Empty)
        {
            item.Id = Guid.NewGuid();
        }

        Run(() => _repository.InsertAsync(ToLocal(item, userId)));
    }

    public void Update(OrderHeaderItem item)
    {
        var userId = CurrentUserId
            ?? throw new InvalidOperationException("Нет текущего пользователя для обновления заказа.");

        Run(() => _repository.UpdateAsync(ToLocal(item, userId)));
    }

    public void Delete(Guid id) =>
        Run(() => _repository.SoftDeleteAsync(id));

    public void UpsertFromProject(ProjectDocumentData project, string filePath)
    {
        var header = project.Order.Header;
        var item = new OrderHeaderItem
        {
            Id = project.Order.Id == Guid.Empty ? Guid.NewGuid() : project.Order.Id,
            OrderNumber = header.OrderNumber,
            DateCreated = header.OrderDate,
            CustomerName = header.CustomerName,
            StructuresCount = project.Order.Constructions.Count,
            TotalSum = ResolveTotalSum(project),
            Status = header.Status,
            FilePath = filePath
        };

        if (GetById(item.Id) is null)
        {
            Add(item);
        }
        else
        {
            Update(item);
        }
    }

    public ProjectDocumentData? TryLoadProject(Guid id) => null;

    public Task RefreshAsync() => Task.CompletedTask;

    public void Save()
    {
        // Persisted immediately via SQLite.
    }

    private static T Run<T>(Func<Task<T>> action) =>
        action().ConfigureAwait(false).GetAwaiter().GetResult();

    private static void Run(Func<Task> action) =>
        action().ConfigureAwait(false).GetAwaiter().GetResult();

    private static OrderHeaderItem ToItem(LocalOrder o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        DateCreated = o.OrderDate,
        CustomerName = o.CustomerName,
        StructuresCount = 0,
        TotalSum = o.AgreedAmount > 0 ? o.AgreedAmount : o.ConstructionsSubtotal,
        Status = (OrderStatus)o.Status,
        FilePath = o.ExportFilePath
    };

    private static LocalOrder ToLocal(OrderHeaderItem item, Guid userId) => new()
    {
        Id = item.Id,
        OrderNumber = item.OrderNumber,
        OrderDate = item.DateCreated,
        CustomerName = item.CustomerName,
        Status = (int)item.Status,
        AgreedAmount = item.TotalSum,
        ConstructionsSubtotal = item.TotalSum,
        ExportFilePath = item.FilePath,
        UserId = userId
    };

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
