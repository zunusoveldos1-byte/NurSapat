using NurSapat.App.Models.Materials;

namespace NurSapat.App.Services.Materials;

public interface IMaterialsDetailStore
{
    IReadOnlyList<MaterialDetail> GetAll();

    MaterialDetail? GetById(Guid id);

    void Add(MaterialDetail material);

    void Update(MaterialDetail material);

    void Delete(Guid id);
}

public sealed class MaterialsDetailStore : IMaterialsDetailStore
{
    private readonly List<MaterialDetail> _items =
    [
        CreateSeed(
            "RH-70-WHT",
            "Профиль Rehau 70 белый",
            "Rehau",
            MaterialSection.Profile,
            MaterialMainUnit.LinearMeter,
            1250m),
        CreateSeed(
            "MACO-EGRESS",
            "Ножницы Maco Multi-Matic",
            "Maco",
            MaterialSection.Hardware,
            MaterialMainUnit.Piece,
            4200m),
        CreateSeed(
            "GL-4-16-4",
            "Стеклопакет 4-16-4",
            "СтеклоТорг",
            MaterialSection.Glazing,
            MaterialMainUnit.SquareMeter,
            2850m,
            isSquare: true)
    ];

    public IReadOnlyList<MaterialDetail> GetAll() => _items;

    public MaterialDetail? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

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
        string article,
        string name,
        string manufacturer,
        MaterialSection section,
        MaterialMainUnit unit,
        decimal price,
        bool isSquare = false)
    {
        var material = new MaterialDetail
        {
            Article = article,
            Name = name,
            Manufacturer = manufacturer,
            Section = section,
            MainUnit = unit,
            BasePrice = price,
            Currency = MaterialCurrency.Kgs,
            EnableLengthCutOptimization = unit == MaterialMainUnit.LinearMeter,
            EnablePlaneCutOptimization = unit == MaterialMainUnit.SquareMeter,
            LinearGroup = unit == MaterialMainUnit.LinearMeter ? MaterialLinearGroup.Profile : null,
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
