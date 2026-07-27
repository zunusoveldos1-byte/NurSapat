using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Materials;
using NurSapat.App.Services.Dictionaries;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialEditViewModel : ObservableObject
{
    private readonly IManufacturersStore _manufacturersStore;
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public MaterialEditViewModel(IManufacturersStore manufacturersStore)
    {
        _manufacturersStore = manufacturersStore;
        Manufacturers = new ObservableCollection<string>(
            _manufacturersStore.GetAll().Select(item => item.Name).Distinct().OrderBy(name => name));
    }

    public string WindowTitle => _isNew ? "Новый материал" : "Редактор материалов";

    public Guid Id => _id;

    public ObservableCollection<string> Manufacturers { get; }

    public ObservableCollection<MaterialColorPriceViewModel> ColorPrices { get; } = [];

    public ObservableCollection<MaterialDimensionPriceViewModel> DimensionPrices { get; } = [];

    public IReadOnlyList<MaterialSection> Sections { get; } = Enum.GetValues<MaterialSection>();

    public IReadOnlyList<MaterialMainUnit> MainUnits { get; } = Enum.GetValues<MaterialMainUnit>();

    public IReadOnlyList<MaterialLinearGroup> LinearGroups { get; } = Enum.GetValues<MaterialLinearGroup>();

    public IReadOnlyList<MaterialSquareGroup> SquareGroups { get; } = Enum.GetValues<MaterialSquareGroup>();

    public IReadOnlyList<MaterialCurrency> Currencies { get; } = Enum.GetValues<MaterialCurrency>();

    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _suppliersText = string.Empty;

    [ObservableProperty]
    private MaterialSection _section = MaterialSection.Profile;

    [ObservableProperty]
    private MaterialMainUnit _mainUnit = MaterialMainUnit.LinearMeter;

    [ObservableProperty]
    private MaterialLinearGroup _linearGroup = MaterialLinearGroup.Profile;

    [ObservableProperty]
    private MaterialSquareGroup _squareGroup = MaterialSquareGroup.Glass;

    [ObservableProperty]
    private MaterialCurrency _currency = MaterialCurrency.Kgs;

    [ObservableProperty]
    private decimal _basePrice;

    [ObservableProperty]
    private decimal _wastePercent;

    [ObservableProperty]
    private bool _printOnAssemblyDrawing = true;

    [ObservableProperty]
    private bool _printOnCommercialOffer = true;

    [ObservableProperty]
    private bool _printOnShippingSheet;

    [ObservableProperty]
    private bool _groupPrintPositions;

    [ObservableProperty]
    private bool _isWarmProfile;

    [ObservableProperty]
    private decimal _innerCoatingAreaPerMeter;

    [ObservableProperty]
    private decimal _outerCoatingAreaPerMeter;

    [ObservableProperty]
    private bool _enableLengthCutOptimization;

    [ObservableProperty]
    private decimal _barLengthMm = 6500;

    [ObservableProperty]
    private decimal _illiquidThresholdMm = 300;

    [ObservableProperty]
    private decimal _scrapMm = 50;

    [ObservableProperty]
    private bool _enablePlaneCutOptimization;

    [ObservableProperty]
    private decimal _sheetWidthMm = 3210;

    [ObservableProperty]
    private decimal _sheetHeightMm = 2250;

    [ObservableProperty]
    private decimal _glassTint;

    [ObservableProperty]
    private decimal _minimumCalculatedAreaSqM = 0.3m;

    [ObservableProperty]
    private bool _useSecondaryUnit;

    [ObservableProperty]
    private decimal _secondaryUnitCoefficient;

    [ObservableProperty]
    private bool _priceByKilogram;

    public bool IsGroupVisible => MainUnit is MaterialMainUnit.LinearMeter or MaterialMainUnit.SquareMeter;

    public bool IsLinearGroupVisible => MainUnit == MaterialMainUnit.LinearMeter;

    public bool IsSquareGroupVisible => MainUnit == MaterialMainUnit.SquareMeter;

    public bool IsWarmProfileVisible => IsLinearGroupVisible && LinearGroup == MaterialLinearGroup.Profile;

    public bool IsCoatingAreaVisible => IsWarmProfileVisible;

    public bool IsCutLengthBlockVisible => IsLinearGroupVisible;

    public bool IsCutPlaneBlockVisible => IsSquareGroupVisible;

    public bool IsGlassFieldsVisible => IsSquareGroupVisible && SquareGroup == MaterialSquareGroup.Glass;

    public bool IsSecondaryUnitBlockVisible => UseSecondaryUnit;

    public bool IsPriceByKilogramVisible => MainUnit == MaterialMainUnit.LinearMeter && UseSecondaryUnit;

    public string BasePriceDisplay => $"{BasePrice:N2} сом";

    public bool IsArticleValid => !string.IsNullOrWhiteSpace(Article);

    public bool IsNameValid => !string.IsNullOrWhiteSpace(Name);

    public bool CanSave => IsArticleValid && IsNameValid;

    public string ArticleValidationMessage => IsArticleValid ? string.Empty : "Артикул обязателен.";

    public string NameValidationMessage => IsNameValid ? string.Empty : "Наименование обязательно.";

    public void Load(MaterialDetail source, bool isClone)
    {
        _isNew = isClone || source.Id == Guid.Empty;
        _id = isClone ? Guid.NewGuid() : source.Id;
        OnPropertyChanged(nameof(Id));

        Article = isClone ? string.Empty : source.Article;
        Name = isClone ? $"{source.Name} (копия)" : source.Name;
        Note = source.Note;
        Manufacturer = source.Manufacturer;
        SuppliersText = source.SuppliersText;
        Section = source.Section;
        MainUnit = source.MainUnit;
        LinearGroup = source.LinearGroup ?? MaterialLinearGroup.Profile;
        SquareGroup = source.SquareGroup ?? MaterialSquareGroup.Glass;
        Currency = source.Currency;
        BasePrice = source.BasePrice;
        WastePercent = source.WastePercent;
        PrintOnAssemblyDrawing = source.PrintOnAssemblyDrawing;
        PrintOnCommercialOffer = source.PrintOnCommercialOffer;
        PrintOnShippingSheet = source.PrintOnShippingSheet;
        GroupPrintPositions = source.GroupPrintPositions;
        IsWarmProfile = source.IsWarmProfile;
        InnerCoatingAreaPerMeter = source.InnerCoatingAreaPerMeter;
        OuterCoatingAreaPerMeter = source.OuterCoatingAreaPerMeter;
        EnableLengthCutOptimization = source.EnableLengthCutOptimization;
        BarLengthMm = source.BarLengthMm;
        IlliquidThresholdMm = source.IlliquidThresholdMm;
        ScrapMm = source.ScrapMm;
        EnablePlaneCutOptimization = source.EnablePlaneCutOptimization;
        SheetWidthMm = source.SheetWidthMm;
        SheetHeightMm = source.SheetHeightMm;
        GlassTint = source.GlassTint;
        MinimumCalculatedAreaSqM = source.MinimumCalculatedAreaSqM;
        UseSecondaryUnit = source.UseSecondaryUnit;
        SecondaryUnitCoefficient = source.SecondaryUnitCoefficient;
        PriceByKilogram = source.PriceByKilogram;

        ColorPrices.Clear();
        foreach (var color in source.ColorPrices)
        {
            ColorPrices.Add(MaterialColorPriceViewModel.FromModel(color));
        }

        if (ColorPrices.Count == 0)
        {
            ColorPrices.Add(new MaterialColorPriceViewModel
            {
                ColorName = "Белый",
                ColorHex = "#FFFFFF",
                Price = BasePrice,
                IsDefault = true
            });
        }

        DimensionPrices.Clear();
        foreach (var price in source.DimensionPrices)
        {
            DimensionPrices.Add(MaterialDimensionPriceViewModel.FromModel(price));
        }

        NotifyLayoutProperties();
    }

    public MaterialDetail BuildResult() => new()
    {
        Id = _id,
        Article = Article,
        Name = Name,
        Note = Note,
        Manufacturer = Manufacturer,
        SuppliersText = SuppliersText,
        Section = Section,
        MainUnit = MainUnit,
        LinearGroup = IsLinearGroupVisible ? LinearGroup : null,
        SquareGroup = IsSquareGroupVisible ? SquareGroup : null,
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
        ColorPrices = ColorPrices.Select(item => item.ToModel()).ToList(),
        DimensionPrices = DimensionPrices.Select(item => item.ToModel()).ToList()
    };

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        Article = Article.Trim();
        Name = Name.Trim();
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    [RelayCommand]
    private async Task AddColorAsync()
    {
        if (ShowColorDialog is null)
        {
            return;
        }

        var row = await ShowColorDialog();
        if (row is not null)
        {
            ColorPrices.Add(row);
        }
    }

    [RelayCommand]
    private void RemoveSelectedColor(MaterialColorPriceViewModel? row)
    {
        if (row is null || row.IsDefault)
        {
            return;
        }

        ColorPrices.Remove(row);
    }

    [RelayCommand]
    private void AddDimensionPrice()
    {
        DimensionPrices.Add(new MaterialDimensionPriceViewModel
        {
            WidthMm = 1000,
            HeightMm = 1000,
            Price = BasePrice
        });
    }

    public event Action<bool>? RequestClose;

    public Func<Task<MaterialColorPriceViewModel?>>? ShowColorDialog { get; set; }

    partial void OnMainUnitChanged(MaterialMainUnit value) => NotifyLayoutProperties();

    partial void OnLinearGroupChanged(MaterialLinearGroup value) => NotifyLayoutProperties();

    partial void OnSquareGroupChanged(MaterialSquareGroup value) => NotifyLayoutProperties();

    partial void OnUseSecondaryUnitChanged(bool value) => NotifyLayoutProperties();

    partial void OnBasePriceChanged(decimal value) => OnPropertyChanged(nameof(BasePriceDisplay));

    partial void OnArticleChanged(string value) => NotifyValidationProperties();

    partial void OnNameChanged(string value) => NotifyValidationProperties();

    private void NotifyValidationProperties()
    {
        OnPropertyChanged(nameof(IsArticleValid));
        OnPropertyChanged(nameof(IsNameValid));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ArticleValidationMessage));
        OnPropertyChanged(nameof(NameValidationMessage));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void NotifyLayoutProperties()
    {
        OnPropertyChanged(nameof(IsGroupVisible));
        OnPropertyChanged(nameof(IsLinearGroupVisible));
        OnPropertyChanged(nameof(IsSquareGroupVisible));
        OnPropertyChanged(nameof(IsWarmProfileVisible));
        OnPropertyChanged(nameof(IsCoatingAreaVisible));
        OnPropertyChanged(nameof(IsCutLengthBlockVisible));
        OnPropertyChanged(nameof(IsCutPlaneBlockVisible));
        OnPropertyChanged(nameof(IsGlassFieldsVisible));
        OnPropertyChanged(nameof(IsSecondaryUnitBlockVisible));
        OnPropertyChanged(nameof(IsPriceByKilogramVisible));
    }
}
