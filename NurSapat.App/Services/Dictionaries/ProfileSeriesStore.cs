using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IProfileSeriesStore
{
    IReadOnlyList<ProfileSeriesItem> GetAll();

    ProfileSeriesItem? GetById(Guid id);

    void Add(ProfileSeriesItem item);

    void Update(ProfileSeriesItem item);

    void Delete(Guid id);
}

public sealed class ProfileSeriesStore : IProfileSeriesStore
{
    private readonly List<ProfileSeriesItem> _items = CreateDemoSeries();

    public IReadOnlyList<ProfileSeriesItem> GetAll() =>
        _items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public ProfileSeriesItem? GetById(Guid id) =>
        _items.FirstOrDefault(item => item.Id == id);

    public void Add(ProfileSeriesItem item) => _items.Add(item);

    public void Update(ProfileSeriesItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(x => x.Id == id);

    private static List<ProfileSeriesItem> CreateDemoSeries() =>
    [
        new()
        {
            Name = "Galwin 60",
            ManufacturerId = DictionarySeedIds.GalwinId,
            HardwareAxisMm = 13.0
        },
        new()
        {
            Name = "Rehau Delight",
            ManufacturerId = DictionarySeedIds.RehauId,
            HardwareAxisMm = 9.0
        },
        new()
        {
            Name = "KBE Engine",
            ManufacturerId = DictionarySeedIds.KbeId,
            HardwareAxisMm = 13.0
        }
    ];
}
