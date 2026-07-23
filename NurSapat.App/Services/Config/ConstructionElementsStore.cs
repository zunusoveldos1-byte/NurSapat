using System.Text.Json;
using NurSapat.App.Models.Config;
using CoreEnvironmentSettings = NurSapat.Core.Settings.EnvironmentSettings;

namespace NurSapat.App.Services.Config;

public interface IConstructionElementsStore
{
    IReadOnlyList<ConstructionElement> GetAll();

    void ReplaceAll(IEnumerable<ConstructionElement> elements);

    void Save();
}

public sealed class ConstructionElementsStore : IConstructionElementsStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private List<ConstructionElement> _items;

    public ConstructionElementsStore(CoreEnvironmentSettings paths)
    {
        _filePath = Path.Combine(paths.AppDataPath, "construction-elements.json");
        _items = [];
        Load();
    }

    public IReadOnlyList<ConstructionElement> GetAll() =>
        _items.Select(item => item.Clone()).ToList();

    public void ReplaceAll(IEnumerable<ConstructionElement> elements) =>
        _items = elements.Select(item => item.Clone()).ToList();

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(_items, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<List<ConstructionElement>>(json, _jsonOptions);
            if (loaded is not null)
            {
                _items = loaded;
            }
        }
        catch
        {
            // Keep empty list when the file is invalid.
        }
    }
}
