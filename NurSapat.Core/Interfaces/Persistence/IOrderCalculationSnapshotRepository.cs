using NurSapat.Core.Models.Pricing;

namespace NurSapat.Core.Interfaces.Persistence;

public interface IOrderCalculationSnapshotRepository
{
    Task<OrderCalculationSnapshot?> GetLatestByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        OrderCalculationSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
