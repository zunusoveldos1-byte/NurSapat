using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Stock;
using NurSapat.Infrastructure.Persistence.Abstractions;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlStockFinishedGoodsStore : IStockFinishedGoodsStore
{
    private readonly IWarehouseRepository _repository;

    public PostgreSqlStockFinishedGoodsStore(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<FinishedGoodItem> GetAll() =>
        PersistenceSync.Run(() => _repository.GetAllFinishedGoodsAsync())
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public FinishedGoodItem? GetById(Guid id)
    {
        var item = PersistenceSync.Run(() => _repository.GetFinishedGoodByIdAsync(id));
        return item is null ? null : AppPersistenceMappers.FromPersistence(item);
    }

    public void Add(FinishedGoodItem item) =>
        PersistenceSync.Run(() => _repository.SaveFinishedGoodAsync(AppPersistenceMappers.ToPersistence(item)));

    public void Update(FinishedGoodItem item) =>
        PersistenceSync.Run(() => _repository.UpdateFinishedGoodAsync(AppPersistenceMappers.ToPersistence(item)));

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteFinishedGoodAsync(id));
}
