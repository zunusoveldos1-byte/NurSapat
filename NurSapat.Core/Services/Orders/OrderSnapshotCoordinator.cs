using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Pricing;

namespace NurSapat.Core.Services.Orders;

public sealed class OrderSnapshotCoordinator
{
    private readonly IOrderCalculationSnapshotRepository _repository;
    private readonly OrderCalculationSnapshotService _snapshotService;

    public OrderSnapshotCoordinator(
        IOrderCalculationSnapshotRepository repository,
        OrderCalculationSnapshotService snapshotService)
    {
        _repository = repository;
        _snapshotService = snapshotService;
    }

    public async Task<OrderCalculationSnapshot> CaptureAsync(
        Order order,
        Guid priceListVersionId,
        string currencyCode,
        decimal exchangeRate,
        CancellationToken cancellationToken = default)
    {
        var snapshot = _snapshotService.Create(
            order,
            priceListVersionId,
            currencyCode,
            exchangeRate);
        await _repository.SaveAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<OrderCalculationSnapshot> RestoreLatestAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.GetLatestByOrderIdAsync(order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Для заказа отсутствует расчетный Snapshot.");
        _snapshotService.Restore(order, snapshot);
        return snapshot;
    }
}
