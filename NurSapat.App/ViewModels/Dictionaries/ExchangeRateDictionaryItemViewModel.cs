using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ExchangeRateDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private decimal _usdRate;

    [ObservableProperty]
    private decimal _eurRate;

    [ObservableProperty]
    private decimal _cnyRate;

    [ObservableProperty]
    private decimal _ueRate;

    public string StartDateDisplay => StartDate.ToString("dd.MM.yyyy");

    public string UsdRateDisplay => $"{UsdRate:N4}";

    public string EurRateDisplay => $"{EurRate:N4}";

    public string CnyRateDisplay => $"{CnyRate:N4}";

    public string UeRateDisplay => $"{UeRate:N4}";

    partial void OnStartDateChanged(DateTime value) => OnPropertyChanged(nameof(StartDateDisplay));

    partial void OnUsdRateChanged(decimal value) => OnPropertyChanged(nameof(UsdRateDisplay));

    partial void OnEurRateChanged(decimal value) => OnPropertyChanged(nameof(EurRateDisplay));

    partial void OnCnyRateChanged(decimal value) => OnPropertyChanged(nameof(CnyRateDisplay));

    partial void OnUeRateChanged(decimal value) => OnPropertyChanged(nameof(UeRateDisplay));

    public static ExchangeRateDictionaryItemViewModel FromModel(ExchangeRatePackage package) => new()
    {
        Id = package.Id,
        StartDate = package.StartDate,
        UsdRate = package.UsdRate,
        EurRate = package.EurRate,
        CnyRate = package.CnyRate,
        UeRate = package.UeRate
    };

    public ExchangeRatePackage ToModel() => new()
    {
        Id = Id,
        StartDate = StartDate,
        UsdRate = UsdRate,
        EurRate = EurRate,
        CnyRate = CnyRate,
        UeRate = UeRate
    };

    public void RefreshFrom(ExchangeRatePackage package)
    {
        StartDate = package.StartDate;
        UsdRate = package.UsdRate;
        EurRate = package.EurRate;
        CnyRate = package.CnyRate;
        UeRate = package.UeRate;
    }
}
