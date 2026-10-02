using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Abstractions;

public interface IErpMaterialRepository
{
    Task<ErpMaterialDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ErpMaterialDetail?> GetByArticleAsync(string article, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpMaterialDetail>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ErpMaterialDetail material, CancellationToken cancellationToken = default);

    Task UpdateAsync(ErpMaterialDetail material, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConstructionElementModel>> GetConstructionElementsAsync(CancellationToken cancellationToken = default);

    Task SaveConstructionElementAsync(ConstructionElementModel element, CancellationToken cancellationToken = default);

    Task UpdateConstructionElementAsync(ConstructionElementModel element, CancellationToken cancellationToken = default);

    Task DeleteConstructionElementAsync(Guid id, CancellationToken cancellationToken = default);
}
