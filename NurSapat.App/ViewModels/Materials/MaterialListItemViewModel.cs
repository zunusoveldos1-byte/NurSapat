using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Materials;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialListItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private int _indexNumber;

    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _suppliersText = string.Empty;

    [ObservableProperty]
    private MaterialSection _section;

    [ObservableProperty]
    private MaterialMainUnit _mainUnit;

    [ObservableProperty]
    private MaterialCurrency _currency;

    [ObservableProperty]
    private decimal _basePrice;

    [ObservableProperty]
    private decimal _wastePercent;

    [ObservableProperty]
    private MaterialLinearGroup? _linearGroup;

    [ObservableProperty]
    private MaterialSquareGroup? _squareGroup;

    public string SectionDisplay => MaterialEnumLabels.Section(Section);

    public string UnitDisplay => MaterialEnumLabels.MainUnit(MainUnit);

    public string CurrencyDisplay => MaterialEnumLabels.Currency(Currency);

    public string BasePriceFormatted => $"{BasePrice:N2} {CurrencyDisplay}";

    public string PriceDisplay => BasePriceFormatted;

    public string GroupDisplay => MainUnit switch
    {
        MaterialMainUnit.LinearMeter when LinearGroup.HasValue => MaterialEnumLabels.LinearGroup(LinearGroup.Value),
        MaterialMainUnit.SquareMeter when SquareGroup.HasValue => MaterialEnumLabels.SquareGroup(SquareGroup.Value),
        _ => "—"
    };

    partial void OnSectionChanged(MaterialSection value) => OnPropertyChanged(nameof(SectionDisplay));

    partial void OnMainUnitChanged(MaterialMainUnit value)
    {
        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(GroupDisplay));
    }

    partial void OnCurrencyChanged(MaterialCurrency value)
    {
        OnPropertyChanged(nameof(CurrencyDisplay));
        OnPropertyChanged(nameof(BasePriceFormatted));
        OnPropertyChanged(nameof(PriceDisplay));
    }

    partial void OnBasePriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(BasePriceFormatted));
        OnPropertyChanged(nameof(PriceDisplay));
    }

    partial void OnLinearGroupChanged(MaterialLinearGroup? value) => OnPropertyChanged(nameof(GroupDisplay));

    partial void OnSquareGroupChanged(MaterialSquareGroup? value) => OnPropertyChanged(nameof(GroupDisplay));

    public static MaterialListItemViewModel FromModel(MaterialDetail detail) => new()
    {
        Id = detail.Id,
        Article = detail.Article,
        Name = detail.Name,
        Manufacturer = detail.Manufacturer,
        SuppliersText = detail.SuppliersText,
        Section = detail.Section,
        MainUnit = detail.MainUnit,
        LinearGroup = detail.LinearGroup,
        SquareGroup = detail.SquareGroup,
        Currency = detail.Currency,
        BasePrice = detail.BasePrice,
        WastePercent = detail.WastePercent
    };

    public void RefreshFrom(MaterialDetail detail)
    {
        Article = detail.Article;
        Name = detail.Name;
        Manufacturer = detail.Manufacturer;
        SuppliersText = detail.SuppliersText;
        Section = detail.Section;
        MainUnit = detail.MainUnit;
        LinearGroup = detail.LinearGroup;
        SquareGroup = detail.SquareGroup;
        Currency = detail.Currency;
        BasePrice = detail.BasePrice;
        WastePercent = detail.WastePercent;
    }
}
