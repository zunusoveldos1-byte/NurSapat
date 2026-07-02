using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IManufacturersStore
{
    IReadOnlyList<ManufacturerItem> GetAll();

    ManufacturerItem? GetById(Guid id);

    void Update(ManufacturerItem item);
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
