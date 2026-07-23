using System.Reflection;
using NurSapat.App.Models.Orders;
using NurSapat.App.Services.Orders;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Serialization;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlOrdersStore : IOrdersStore
{
    private readonly IOrderRepository _repository;

    public PostgreSqlOrdersStore(IOrderRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<OrderHeaderItem> GetAll() =>
        PersistenceSync.Run(() => _repository.GetAllAsync())
            .Select(ToHeader)
            .OrderByDescending(item => item.DateCreated)
            .ToList();

    public OrderHeaderItem? GetById(Guid id)
    {
        var order = PersistenceSync.Run(() => _repository.GetByIdAsync(id));
        return order is null ? null : ToHeader(order);
    }

    public void Add(OrderHeaderItem item)
    {
        var order = CreateMinimalOrder(item);
        PersistenceSync.Run(() => _repository.SaveAsync(order));
    }

    public void Update(OrderHeaderItem item)
    {
        var existing = PersistenceSync.Run(() => _repository.GetByIdAsync(item.Id));
        if (existing is null)
        {
            Add(item);
            return;
        }

        ApplyHeader(existing, item);
        PersistenceSync.Run(() => _repository.UpdateAsync(existing));
    }

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteAsync(id));

    public void UpsertFromProject(ProjectDocumentData project, string filePath)
    {
        var order = ProjectOrderMapper.FromProject(project);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            order.ExportFilePath = filePath;
        }

        var existing = PersistenceSync.Run(() => _repository.GetByIdAsync(order.Id));
        if (existing is null)
        {
            PersistenceSync.Run(() => _repository.SaveAsync(order));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(order.ExportFilePath))
            {
                order.ExportFilePath = existing.ExportFilePath;
            }

            PersistenceSync.Run(() => _repository.UpdateAsync(order));
        }
    }

    public Task RefreshAsync() => Task.CompletedTask;

    public void Save()
    {
        // EF repositories persist immediately.
    }

    public ProjectDocumentData? TryLoadProject(Guid id)
    {
        var order = PersistenceSync.Run(() => _repository.GetByIdAsync(id));
        return order is null ? null : ProjectOrderMapper.ToProject(order);
    }

    private static OrderHeaderItem ToHeader(Order order)
    {
        var total = order.Payment.AgreedAmount > 0
            ? order.Payment.AgreedAmount
            : order.Totals.ConstructionsSubtotal
              + order.Totals.DeliveryCost
              + order.Totals.AdditionalMaterialsCost
              + order.Totals.AdditionalMaterialsInstallationCost;

        return new OrderHeaderItem
        {
            Id = order.Id,
            OrderNumber = order.Header.OrderNumber,
            DateCreated = order.Header.OrderDate == default ? DateTime.Today : order.Header.OrderDate,
            CustomerName = order.Header.CustomerName,
            StructuresCount = order.Constructions.Count,
            TotalSum = total,
            Status = order.Header.Status,
            FilePath = order.ExportFilePath
        };
    }

    private static Order CreateMinimalOrder(OrderHeaderItem item)
    {
        var order = new Order { Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id };
        ApplyHeader(order, item);
        order.EnsureDefaultConstruction();
        if (item.TotalSum > 0)
        {
            order.Payment.AgreedAmount = item.TotalSum;
        }

        return order;
    }

    private static void ApplyHeader(Order order, OrderHeaderItem item)
    {
        order.Header.OrderNumber = string.IsNullOrWhiteSpace(item.OrderNumber)
            ? order.Header.OrderNumber
            : item.OrderNumber;
        order.Header.OrderDate = item.DateCreated == default ? order.Header.OrderDate : item.DateCreated;
        order.Header.CustomerName = item.CustomerName;
        order.Header.Status = item.Status;
        if (!string.IsNullOrWhiteSpace(item.FilePath))
        {
            order.ExportFilePath = item.FilePath;
        }
    }
}

/// <summary>Maps between ProjectDocumentData (.nsp / workspace) and domain Order for PostgreSQL persistence.</summary>
public static class ProjectOrderMapper
{
    public static Order FromProject(ProjectDocumentData project)
    {
        var id = project.Order.Id == Guid.Empty ? Guid.NewGuid() : project.Order.Id;
        var order = new Order { Id = id };
        OrderProjectMapper.ApplyData(order, project.Order);
        order.Id = id;
        return order;
    }

    public static ProjectDocumentData ToProject(Order order) => new()
    {
        SchemaVersion = ProjectFormat.CurrentSchemaVersion,
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        SavedAt = DateTimeOffset.UtcNow,
        Order = OrderProjectMapper.ToData(order),
        Cad = new CadProjectData(),
        PrintSettings = new PrintSettings(),
        ActiveConstructionId = order.SelectedConstruction?.Id
            ?? order.Constructions.FirstOrDefault()?.Id
    };
}
