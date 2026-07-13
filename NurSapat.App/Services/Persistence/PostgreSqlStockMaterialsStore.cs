using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Stock;
using NurSapat.Infrastructure.Persistence.Abstractions;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlStockMaterialsStore : IStockMaterialsStore
{
    private readonly IWarehouseRepository _repository;

    public PostgreSqlStockMaterialsStore(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<StockDocumentHeader> GetAll() =>
        _repository.GetAllDocumentsAsync().GetAwaiter().GetResult()
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public StockDocumentHeader? GetById(Guid id)
    {
        var document = _repository.GetDocumentByIdAsync(id).GetAwaiter().GetResult();
        return document is null ? null : AppPersistenceMappers.FromPersistence(document);
    }

    public void Add(StockDocumentHeader document) =>
        _repository.SaveDocumentAsync(AppPersistenceMappers.ToPersistence(document)).GetAwaiter().GetResult();

    public void Update(StockDocumentHeader document) =>
        _repository.UpdateDocumentAsync(AppPersistenceMappers.ToPersistence(document)).GetAwaiter().GetResult();

    public void Delete(Guid id) =>
        _repository.DeleteDocumentAsync(id).GetAwaiter().GetResult();

    public StockDocumentHeader? GetMinStockLimitDocument() =>
        GetAll().FirstOrDefault(document => document.Type == StockDocumentType.MinStockLimit);

    public int GetNextSequence() =>
        GetAll().Count + 1;
}
