using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Interfaces.Persistence;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
