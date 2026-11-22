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
        PersistenceSync.Run(() => _repository.GetAllDocumentsAsync())
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public StockDocumentHeader? GetById(Guid id)
    {
        var document = PersistenceSync.Run(() => _repository.GetDocumentByIdAsync(id));
        return document is null ? null : AppPersistenceMappers.FromPersistence(document);
    }

    public void Add(StockDocumentHeader document) =>
        PersistenceSync.Run(() => _repository.SaveDocumentAsync(AppPersistenceMappers.ToPersistence(document)));

    public void Update(StockDocumentHeader document) =>
        PersistenceSync.Run(() => _repository.UpdateDocumentAsync(AppPersistenceMappers.ToPersistence(document)));

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteDocumentAsync(id));

    public StockDocumentHeader? GetMinStockLimitDocument() =>
        GetAll().FirstOrDefault(document => document.Type == StockDocumentType.MinStockLimit);

    public int GetNextSequence() =>
        GetAll().Count + 1;
}
