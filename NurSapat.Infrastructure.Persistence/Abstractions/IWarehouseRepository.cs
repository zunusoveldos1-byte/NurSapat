using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Abstractions;

public interface IWarehouseRepository
{
    Task<StockDocumentModel?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockDocumentModel>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);

    Task SaveDocumentAsync(StockDocumentModel document, CancellationToken cancellationToken = default);

    Task UpdateDocumentAsync(StockDocumentModel document, CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FinishedGoodModel?> GetFinishedGoodByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinishedGoodModel>> GetAllFinishedGoodsAsync(CancellationToken cancellationToken = default);

    Task SaveFinishedGoodAsync(FinishedGoodModel item, CancellationToken cancellationToken = default);

    Task UpdateFinishedGoodAsync(FinishedGoodModel item, CancellationToken cancellationToken = default);

    Task DeleteFinishedGoodAsync(Guid id, CancellationToken cancellationToken = default);
}
