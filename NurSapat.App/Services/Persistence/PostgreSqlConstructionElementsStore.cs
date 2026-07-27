using NurSapat.App.Models.Config;
using NurSapat.App.Services.Config;
using NurSapat.Infrastructure.Persistence.Abstractions;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlConstructionElementsStore : IConstructionElementsStore
{
    private readonly IErpMaterialRepository _repository;

    public PostgreSqlConstructionElementsStore(IErpMaterialRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<ConstructionElement> GetAll() =>
        PersistenceSync.Run(() => _repository.GetConstructionElementsAsync())
            .Select(AppPersistenceMappers.FromPersistence)
            .ToList();

    public void ReplaceAll(IEnumerable<ConstructionElement> elements)
    {
        var existing = PersistenceSync.Run(() => _repository.GetConstructionElementsAsync());
        foreach (var element in existing)
        {
            PersistenceSync.Run(() => _repository.DeleteConstructionElementAsync(element.Id));
        }

        foreach (var element in elements)
        {
            PersistenceSync.Run(() =>
                _repository.SaveConstructionElementAsync(AppPersistenceMappers.ToPersistence(element)));
        }
    }

    public void Save()
    {
        // EF repositories persist immediately on ReplaceAll / individual writes.
    }
}
