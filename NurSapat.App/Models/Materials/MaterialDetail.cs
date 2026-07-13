namespace NurSapat.App.Models.Materials;

using NurSapat.Core.Validation;

/// <summary>Каноническая ERP-сущность материала (цены, единицы, склад, спецификация). Ключ — <see cref="Id"/> + <see cref="Article"/>.</summary>
public sealed class MaterialDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public MaterialSection Section { get; set; } = MaterialSection.Profile;

    public MaterialMainUnit MainUnit { get; set; } = MaterialMainUnit.LinearMeter;

    public MaterialLinearGroup? LinearGroup { get; set; } = MaterialLinearGroup.Profile;

    public MaterialSquareGroup? SquareGroup { get; set; }

    public MaterialCurrency Currency { get; set; } = MaterialCurrency.Kgs;

    public decimal BasePrice { get; set; }

    public decimal WastePercent { get; set; }

    public bool PrintOnAssemblyDrawing { get; set; } = true;

    public bool PrintOnCommercialOffer { get; set; } = true;

    public bool PrintOnShippingSheet { get; set; }

    public bool GroupPrintPositions { get; set; }

    public bool IsWarmProfile { get; set; }

    public decimal InnerCoatingAreaPerMeter { get; set; }

    public decimal OuterCoatingAreaPerMeter { get; set; }

    public bool EnableLengthCutOptimization { get; set; }

    public decimal BarLengthMm { get; set; } = 6500;

    public decimal IlliquidThresholdMm { get; set; } = 300;

    public decimal ScrapMm { get; set; } = 50;

    public bool EnablePlaneCutOptimization { get; set; }

    public decimal SheetWidthMm { get; set; } = 3210;

    public decimal SheetHeightMm { get; set; } = 2250;

    public decimal GlassTint { get; set; }

    public decimal MinimumCalculatedAreaSqM { get; set; } = 0.3m;

    public bool UseSecondaryUnit { get; set; }

    public decimal SecondaryUnitCoefficient { get; set; }

    public bool PriceByKilogram { get; set; }

    public List<MaterialColorPrice> ColorPrices { get; set; } = [];

    public List<MaterialDimensionPrice> DimensionPrices { get; set; } = [];

    public void Validate()
    {
        DomainInvariants.RequireNonEmptyGuid(Id, nameof(Id));
        DomainInvariants.RequireNonEmpty(Article, nameof(Article));
        DomainInvariants.RequireNonEmpty(Name, nameof(Name));
        DomainInvariants.RequireNonNegative(BasePrice, nameof(BasePrice));
        DomainInvariants.RequireNonNegative(WastePercent, nameof(WastePercent));
    }

    public MaterialDetail Clone(bool newId = true)
    {
        var clone = new MaterialDetail
        {
            Id = newId ? Guid.NewGuid() : Id,
            Article = Article,
            Name = Name,
            Note = Note,
            Manufacturer = Manufacturer,
            Section = Section,
            MainUnit = MainUnit,
            LinearGroup = LinearGroup,
            SquareGroup = SquareGroup,
            Currency = Currency,
            BasePrice = BasePrice,
            WastePercent = WastePercent,
            PrintOnAssemblyDrawing = PrintOnAssemblyDrawing,
            PrintOnCommercialOffer = PrintOnCommercialOffer,
            PrintOnShippingSheet = PrintOnShippingSheet,
            GroupPrintPositions = GroupPrintPositions,
            IsWarmProfile = IsWarmProfile,
            InnerCoatingAreaPerMeter = InnerCoatingAreaPerMeter,
            OuterCoatingAreaPerMeter = OuterCoatingAreaPerMeter,
            EnableLengthCutOptimization = EnableLengthCutOptimization,
            BarLengthMm = BarLengthMm,
            IlliquidThresholdMm = IlliquidThresholdMm,
            ScrapMm = ScrapMm,
            EnablePlaneCutOptimization = EnablePlaneCutOptimization,
            SheetWidthMm = SheetWidthMm,
            SheetHeightMm = SheetHeightMm,
            GlassTint = GlassTint,
            MinimumCalculatedAreaSqM = MinimumCalculatedAreaSqM,
            UseSecondaryUnit = UseSecondaryUnit,
            SecondaryUnitCoefficient = SecondaryUnitCoefficient,
            PriceByKilogram = PriceByKilogram,
            ColorPrices = ColorPrices.Select(color => new MaterialColorPrice
            {
                Id = newId ? Guid.NewGuid() : color.Id,
                ColorArticle = color.ColorArticle,
                ColorName = color.ColorName,
                ColorHex = color.ColorHex,
                Price = color.Price,
                LinkArticle = color.LinkArticle,
                IsDefault = color.IsDefault,
                UseCustomCutLength = color.UseCustomCutLength,
                CustomBarLengthMm = color.CustomBarLengthMm
            }).ToList(),
            DimensionPrices = DimensionPrices.Select(price => new MaterialDimensionPrice
            {
                Id = newId ? Guid.NewGuid() : price.Id,
                WidthMm = price.WidthMm,
                HeightMm = price.HeightMm,
                Price = price.Price
            }).ToList()
        };

        return clone;
    }

    public static MaterialDetail CreateDefault()
    {
        var material = new MaterialDetail
        {
            Article = "NEW-001",
            Name = "Новый материал",
            Manufacturer = "Rehau",
            Section = MaterialSection.Profile,
            MainUnit = MaterialMainUnit.LinearMeter,
            LinearGroup = MaterialLinearGroup.Profile,
            BasePrice = 0,
            EnableLengthCutOptimization = true
        };

        material.ColorPrices.Add(new MaterialColorPrice
        {
            ColorName = "Белый",
            ColorHex = "#FFFFFF",
            Price = 0,
            IsDefault = true
        });

        return material;
    }
}
