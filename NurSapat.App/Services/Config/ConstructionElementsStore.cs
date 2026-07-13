using System.Text.Json;
using NurSapat.App.Models.Config;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Core.Models.Materials;
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
        _items = CreateDemoElements();
        Load();
    }

    public IReadOnlyList<ConstructionElement> GetAll() =>
        _items.Select(item => item.Clone()).ToList();

    public void ReplaceAll(IEnumerable<ConstructionElement> elements) =>
        _items = elements.Select(item => item.Clone()).ToList();

    public void Save()
    {
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
            if (loaded is { Count: > 0 })
            {
                _items = loaded;
            }
        }
        catch
        {
            // Keep demo seed data when the file is invalid.
        }
    }

    private static List<ConstructionElement> CreateDemoElements() =>
    [
            new()
            {
                MaterialId = CatalogMaterialIds.ProfileRehau70,
                Name = "Рама Galwin 60",
                ManufacturerId = DictionarySeedIds.GalwinId,
                Section = ConstructionSectionType.Frame,
                AssociatedSeriesIds = [],
                WidthMm = 60,
                HeightMm = 76,
                WallThicknessMm = 2.8,
                Article = CatalogMaterialArticles.ProfileRehau70,
                WeightKg = 1.12
            },
            new()
            {
                MaterialId = CatalogMaterialIds.ProfileRehau70,
                Name = "Створка Galwin 60",
                ManufacturerId = DictionarySeedIds.GalwinId,
                Section = ConstructionSectionType.Sash,
                AssociatedSeriesIds = [],
                WidthMm = 60,
                HeightMm = 68,
                WallThicknessMm = 2.5,
                Article = CatalogMaterialArticles.ProfileRehau70,
                WeightKg = 0.98
            },
            new()
            {
                MaterialId = CatalogMaterialIds.ProfileRehau70,
                Name = "Импост Galwin 60",
                ManufacturerId = DictionarySeedIds.GalwinId,
                Section = ConstructionSectionType.Impost,
                AssociatedSeriesIds = [],
                WidthMm = 60,
                HeightMm = 60,
                WallThicknessMm = 2.5,
                Article = CatalogMaterialArticles.ProfileRehau70,
                WeightKg = 0.85
            },
            new()
            {
                MaterialId = CatalogMaterialIds.Glazing4_16_4,
                Name = "4-16-4",
                ManufacturerId = DictionarySeedIds.GalwinId,
                Section = ConstructionSectionType.Glazing,
                Article = CatalogMaterialArticles.Glazing4_16_4,
                GlassFormula = "4-16-4",
                GlassThicknessMm = 24,
                GlassStructure = "Стекло-аргон-стекло"
            },
            new()
            {
                MaterialId = CatalogMaterialIds.HardwareMacoEgress,
                Name = "Комплект Maco",
                ManufacturerId = DictionarySeedIds.MacoId,
                Section = ConstructionSectionType.Hardware,
                Article = CatalogMaterialArticles.HardwareMacoEgress,
                HardwareRules =
                [
                    new HardwareAutoPickRule
                    {
                        MinWidthFalzMm = 400,
                        MaxWidthFalzMm = 800,
                        MinHeightFalzMm = 600,
                        MaxHeightFalzMm = 1400,
                        ScissorsCount = 1,
                        MiddleLockCount = 1
                    },
                    new HardwareAutoPickRule
                    {
                        MinWidthFalzMm = 801,
                        MaxWidthFalzMm = 1400,
                        MinHeightFalzMm = 600,
                        MaxHeightFalzMm = 2200,
                        ScissorsCount = 2,
                        MiddleLockCount = 2
                    }
                ]
            }
        ];
}
