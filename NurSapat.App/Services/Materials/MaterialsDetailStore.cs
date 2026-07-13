using NurSapat.App.Models.Materials;
using NurSapat.Core.Models.Materials;

namespace NurSapat.App.Services.Materials;

public interface IMaterialsDetailStore
{
    IReadOnlyList<MaterialDetail> GetAll();

    MaterialDetail? GetById(Guid id);

    MaterialDetail? GetByArticle(string article);

    void Add(MaterialDetail material);

    void Update(MaterialDetail material);

    void Delete(Guid id);
}

public sealed class MaterialsDetailStore : IMaterialsDetailStore
{
    private readonly List<MaterialDetail> _items =
    [
        CreateSeed(
            CatalogMaterialIds.ProfileRehau70,
            CatalogMaterialArticles.ProfileRehau70,
            "Профиль Rehau 70 белый",
            "Rehau",
            MaterialSection.Profile,
            MaterialMainUnit.LinearMeter,
            1250m),
        CreateSeed(
            CatalogMaterialIds.HardwareMacoEgress,
            CatalogMaterialArticles.HardwareMacoEgress,
            "Ножницы Maco Multi-Matic",
            "Maco",
            MaterialSection.Hardware,
            MaterialMainUnit.Piece,
            4200m),
        CreateSeed(
            CatalogMaterialIds.Glazing4_16_4,
            CatalogMaterialArticles.Glazing4_16_4,
            "Стеклопакет 4-16-4",
            "СтеклоТорг",
            MaterialSection.Glazing,
            MaterialMainUnit.SquareMeter,
            2850m,
            isSquare: true),
        CreateSeed(
            CatalogMaterialIds.SealantEpdm,
            CatalogMaterialArticles.SealantEpdm,
            "Уплотнитель EPDM",
            "Rehau",
            MaterialSection.Sealants,
            MaterialMainUnit.LinearMeter,
            85m),
        CreateSeed(
            CatalogMaterialIds.Reinforcement12,
            CatalogMaterialArticles.Reinforcement12,
            "Армирование 1.2 мм",
            "Rehau",
            MaterialSection.Profile,
            MaterialMainUnit.LinearMeter,
            320m,
            linearGroup: MaterialLinearGroup.Reinforcement),
        CreateSeed(
            CatalogMaterialIds.Connector90,
            CatalogMaterialArticles.Connector90,
            "Соединитель 90°",
            "Rehau",
            MaterialSection.Components,
            MaterialMainUnit.Piece,
            180m),
        CreateSeed(
            CatalogMaterialIds.Bead24,
            CatalogMaterialArticles.Bead24,
            "Штапик 24 мм",
            "Rehau",
            MaterialSection.Profile,
            MaterialMainUnit.LinearMeter,
            95m,
            linearGroup: MaterialLinearGroup.Bead),
        CreateSeed(
            CatalogMaterialIds.MosquitoNetFiberglass,
            CatalogMaterialArticles.MosquitoNetFiberglass,
            "Москитная сетка Fiberglass",
            "Fiberglass",
            MaterialSection.Components,
            MaterialMainUnit.SquareMeter,
            650m,
            isSquare: true),
        CreateSeed(
            CatalogMaterialIds.SillPvc300,
            CatalogMaterialArticles.SillPvc300,
            "Подоконник ПВХ 300 мм",
            "Rehau",
            MaterialSection.Components,
            MaterialMainUnit.LinearMeter,
            890m),
        CreateSeed(
            CatalogMaterialIds.SillAluWhite,
            CatalogMaterialArticles.SillAluWhite,
            "Подоконник алюминий белый",
            "Alutech",
            MaterialSection.Components,
            MaterialMainUnit.LinearMeter,
            1120m),
        CreateSeed(
            CatalogMaterialIds.Spoke8White,
            CatalogMaterialArticles.Spoke8White,
            "Шпрос 8 мм (белый)",
            "Rehau",
            MaterialSection.Components,
            MaterialMainUnit.LinearMeter,
            420m)
    ];

    public IReadOnlyList<MaterialDetail> GetAll() => _items;

    public MaterialDetail? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

    public MaterialDetail? GetByArticle(string article) =>
        _items.FirstOrDefault(item => string.Equals(item.Article, article, StringComparison.OrdinalIgnoreCase));

    public void Add(MaterialDetail material) => _items.Add(material);

    public void Update(MaterialDetail material)
    {
        var index = _items.FindIndex(item => item.Id == material.Id);
        if (index >= 0)
        {
            _items[index] = material;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(item => item.Id == id);

    private static MaterialDetail CreateSeed(
        Guid id,
        string article,
        string name,
        string manufacturer,
        MaterialSection section,
        MaterialMainUnit unit,
        decimal price,
        bool isSquare = false,
        MaterialLinearGroup? linearGroup = null)
    {
        var material = new MaterialDetail
        {
            Id = id,
            Article = article,
            Name = name,
            Manufacturer = manufacturer,
            Section = section,
            MainUnit = unit,
            BasePrice = price,
            Currency = MaterialCurrency.Kgs,
            EnableLengthCutOptimization = unit == MaterialMainUnit.LinearMeter,
            EnablePlaneCutOptimization = unit == MaterialMainUnit.SquareMeter,
            LinearGroup = linearGroup ?? (unit == MaterialMainUnit.LinearMeter ? MaterialLinearGroup.Profile : null),
            SquareGroup = isSquare ? MaterialSquareGroup.Glass : null,
            PrintOnAssemblyDrawing = true,
            PrintOnCommercialOffer = true
        };

        material.ColorPrices.Add(new MaterialColorPrice
        {
            ColorName = "Белый",
            ColorHex = "#FFFFFF",
            Price = price,
            IsDefault = true
        });

        if (isSquare)
        {
            material.DimensionPrices.Add(new MaterialDimensionPrice
            {
                WidthMm = 2000,
                HeightMm = 1500,
                Price = price + 150
            });
        }

        return material;
    }
}
