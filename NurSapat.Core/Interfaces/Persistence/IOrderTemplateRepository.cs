using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Interfaces.Persistence;

public interface IOrderTemplateRepository
{
    Task<OrderTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(OrderTemplate template, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrderTemplate template, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
