using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IManufacturersStore
{
    IReadOnlyList<ManufacturerItem> GetAll();

    ManufacturerItem? GetById(Guid id);

    void Add(ManufacturerItem item);

    void Update(ManufacturerItem item);

    void Delete(Guid id);
}

public sealed class ManufacturersStore : IManufacturersStore
{
    private readonly List<ManufacturerItem> _items = CreateFixedManufacturers();

    public IReadOnlyList<ManufacturerItem> GetAll() =>
        _items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public ManufacturerItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public void Update(ManufacturerItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Add(ManufacturerItem item) => _items.Add(item);

    public void Delete(Guid id) => _items.RemoveAll(item => item.Id == id);

    private static List<ManufacturerItem> CreateFixedManufacturers() =>
    [
        new()
        {
            Id = DictionarySeedIds.MacoId,
            Name = "Maco",
            IsActive = true,
            FalsCorrectionMm = 0.5,
            EndCuttingMm = 2.0,
            BladeWidthMm = 3.0,
            WeldingShrinkageMm = 1.5
        },
        new()
        {
            Id = DictionarySeedIds.GalwinId,
            Name = "Galwin",
            IsActive = true,
            FalsCorrectionMm = 0.0,
            EndCuttingMm = 2.5,
            BladeWidthMm = 3.2,
            WeldingShrinkageMm = 2.0
        },
        new()
        {
            Id = DictionarySeedIds.RehauId,
            Name = "Rehau",
            IsActive = true,
            FalsCorrectionMm = 0.3,
            EndCuttingMm = 2.0,
            BladeWidthMm = 3.0,
            WeldingShrinkageMm = 1.8
        },
        new()
        {
            Id = DictionarySeedIds.RotoId,
            Name = "Roto",
            IsActive = true,
            FalsCorrectionMm = 0.4,
            EndCuttingMm = 2.0,
            BladeWidthMm = 3.0,
            WeldingShrinkageMm = 1.6
        },
        new()
        {
            Id = DictionarySeedIds.KbeId,
            Name = "KBE",
            IsActive = true,
            FalsCorrectionMm = 0.2,
            EndCuttingMm = 2.0,
            BladeWidthMm = 3.0,
            WeldingShrinkageMm = 1.7
        }
    ];
}
