using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class OrderTemplateDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Guid? _manufacturerId;

    [ObservableProperty]
    private string _manufacturerDisplay = "Все";

    [ObservableProperty]
    private string _currencyCode = "KGS";

    [ObservableProperty]
    private decimal _installationPrice;

    [ObservableProperty]
    private decimal _profitMargin;

    public string InstallationPriceDisplay => $"{InstallationPrice:N2}";

    public string ProfitMarginDisplay => $"{ProfitMargin:N2} %";

    partial void OnInstallationPriceChanged(decimal value) => OnPropertyChanged(nameof(InstallationPriceDisplay));

    partial void OnProfitMarginChanged(decimal value) => OnPropertyChanged(nameof(ProfitMarginDisplay));

    public static OrderTemplateDictionaryItemViewModel FromModel(
        OrderTemplateItem item,
        string manufacturerDisplay) => new()
    {
        Id = item.Id,
        Name = item.Name,
        ManufacturerId = item.ManufacturerId,
        ManufacturerDisplay = manufacturerDisplay,
        CurrencyCode = item.CurrencyCode,
        InstallationPrice = item.InstallationPrice,
        ProfitMargin = item.ProfitMargin
    };

    public OrderTemplateItem ToModel() => new()
    {
        Id = Id,
        Name = Name,
        ManufacturerId = ManufacturerId,
        CurrencyCode = CurrencyCode,
        InstallationPrice = InstallationPrice,
        ProfitMargin = ProfitMargin
    };

    public void RefreshFrom(OrderTemplateItem item, string manufacturerDisplay)
    {
        Name = item.Name;
        ManufacturerId = item.ManufacturerId;
        ManufacturerDisplay = manufacturerDisplay;
        CurrencyCode = item.CurrencyCode;
        InstallationPrice = item.InstallationPrice;
        ProfitMargin = item.ProfitMargin;
    }
}
