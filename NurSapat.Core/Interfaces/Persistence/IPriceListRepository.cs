using NurSapat.Core.Models.Pricing;

namespace NurSapat.Core.Interfaces.Persistence;

public interface IPriceListRepository
{
    Task<PriceListVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(PriceListVersion version, CancellationToken cancellationToken = default);
    Task UpdateAsync(PriceListVersion version, CancellationToken cancellationToken = default);
}
