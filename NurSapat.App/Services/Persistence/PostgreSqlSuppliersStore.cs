using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Core.Interfaces.Persistence;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlSuppliersStore : ISuppliersStore
{
    private readonly ISupplierRepository _repository;

    public PostgreSqlSuppliersStore(ISupplierRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<SupplierItem> GetAll() =>
        PersistenceSync.Run(() => _repository.GetAllAsync())
            .Select(supplier => ToItem(supplier))
            .ToList();

    public SupplierItem? GetById(Guid id)
    {
        var supplier = PersistenceSync.Run(() => _repository.GetByIdAsync(id));
        return supplier is null ? null : ToItem(supplier);
    }

    public void Add(SupplierItem item) =>
        PersistenceSync.Run(() => _repository.SaveAsync(item));

    public void Update(SupplierItem item) =>
        PersistenceSync.Run(() => _repository.UpdateAsync(item));

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteAsync(id));

    private static SupplierItem ToItem(Core.Models.Orders.Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Phone = supplier.Phone,
        LegalAddress = supplier.LegalAddress,
        ContractTerms = supplier.ContractTerms,
        Note = supplier.Note
    };
}
