using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Materials;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialListItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

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

    public string SectionDisplay => MaterialEnumLabels.Section(Section);

    public string UnitDisplay => MaterialEnumLabels.MainUnit(MainUnit);

    public string CurrencyDisplay => MaterialEnumLabels.Currency(Currency);

    public string PriceDisplay => $"{BasePrice:N2} сом";

    partial void OnSectionChanged(MaterialSection value) => OnPropertyChanged(nameof(SectionDisplay));

    partial void OnMainUnitChanged(MaterialMainUnit value) => OnPropertyChanged(nameof(UnitDisplay));

    partial void OnCurrencyChanged(MaterialCurrency value) => OnPropertyChanged(nameof(CurrencyDisplay));

    partial void OnBasePriceChanged(decimal value) => OnPropertyChanged(nameof(PriceDisplay));

    public static MaterialListItemViewModel FromModel(MaterialDetail detail) => new()
    {
        Id = detail.Id,
        Article = detail.Article,
        Name = detail.Name,
        Manufacturer = detail.Manufacturer,
        Section = detail.Section,
        MainUnit = detail.MainUnit,
        Currency = detail.Currency,
        BasePrice = detail.BasePrice,
        WastePercent = detail.WastePercent
    };

    public void RefreshFrom(MaterialDetail detail)
    {
        Article = detail.Article;
        Name = detail.Name;
        Manufacturer = detail.Manufacturer;
        Section = detail.Section;
        MainUnit = detail.MainUnit;
        Currency = detail.Currency;
        BasePrice = detail.BasePrice;
        WastePercent = detail.WastePercent;
    }
}
