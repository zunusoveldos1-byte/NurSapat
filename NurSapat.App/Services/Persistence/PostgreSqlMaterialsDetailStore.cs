using NurSapat.App.Models.Materials;
using NurSapat.App.Services.Materials;
using NurSapat.Infrastructure.Persistence.Abstractions;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlMaterialsDetailStore : IMaterialsDetailStore
{
    private readonly IErpMaterialRepository _repository;

    public PostgreSqlMaterialsDetailStore(IErpMaterialRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<MaterialDetail> GetAll() =>
        PersistenceSync.Run(() => _repository.GetAllAsync())
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public MaterialDetail? GetById(Guid id)
    {
        var material = PersistenceSync.Run(() => _repository.GetByIdAsync(id));
        return material is null ? null : AppPersistenceMappers.FromPersistence(material);
    }

    public MaterialDetail? GetByArticle(string article)
    {
        var material = PersistenceSync.Run(() => _repository.GetByArticleAsync(article));
        return material is null ? null : AppPersistenceMappers.FromPersistence(material);
    }

    public void Add(MaterialDetail material) =>
        PersistenceSync.Run(() => _repository.SaveAsync(AppPersistenceMappers.ToPersistence(material)));

    public void Update(MaterialDetail material) =>
        PersistenceSync.Run(() => _repository.UpdateAsync(AppPersistenceMappers.ToPersistence(material)));

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteAsync(id));
}
