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
        _repository.GetAllAsync().GetAwaiter().GetResult()
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public MaterialDetail? GetById(Guid id)
    {
        var material = _repository.GetByIdAsync(id).GetAwaiter().GetResult();
        return material is null ? null : AppPersistenceMappers.FromPersistence(material);
    }

    public MaterialDetail? GetByArticle(string article)
    {
        var material = _repository.GetByArticleAsync(article).GetAwaiter().GetResult();
        return material is null ? null : AppPersistenceMappers.FromPersistence(material);
    }

    public void Add(MaterialDetail material) =>
        _repository.SaveAsync(AppPersistenceMappers.ToPersistence(material)).GetAwaiter().GetResult();

    public void Update(MaterialDetail material) =>
        _repository.UpdateAsync(AppPersistenceMappers.ToPersistence(material)).GetAwaiter().GetResult();

    public void Delete(Guid id) =>
        _repository.DeleteAsync(id).GetAwaiter().GetResult();
}
