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
        _repository.GetAllFinishedGoodsAsync().GetAwaiter().GetResult()
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public FinishedGoodItem? GetById(Guid id)
    {
        var item = _repository.GetFinishedGoodByIdAsync(id).GetAwaiter().GetResult();
        return item is null ? null : AppPersistenceMappers.FromPersistence(item);
    }

    public void Add(FinishedGoodItem item) =>
        _repository.SaveFinishedGoodAsync(AppPersistenceMappers.ToPersistence(item)).GetAwaiter().GetResult();

    public void Update(FinishedGoodItem item) =>
        _repository.UpdateFinishedGoodAsync(AppPersistenceMappers.ToPersistence(item)).GetAwaiter().GetResult();

    public void Delete(Guid id) =>
        _repository.DeleteFinishedGoodAsync(id).GetAwaiter().GetResult();
}
