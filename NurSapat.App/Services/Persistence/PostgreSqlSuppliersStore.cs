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
        _repository.GetAllAsync().GetAwaiter().GetResult()
            .Select(supplier => ToItem(supplier))
            .ToList();

    public SupplierItem? GetById(Guid id)
    {
        var supplier = _repository.GetByIdAsync(id).GetAwaiter().GetResult();
        return supplier is null ? null : ToItem(supplier);
    }

    public void Add(SupplierItem item) =>
        _repository.SaveAsync(item).GetAwaiter().GetResult();

    public void Update(SupplierItem item) =>
        _repository.UpdateAsync(item).GetAwaiter().GetResult();

    public void Delete(Guid id) =>
        _repository.DeleteAsync(id).GetAwaiter().GetResult();

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
