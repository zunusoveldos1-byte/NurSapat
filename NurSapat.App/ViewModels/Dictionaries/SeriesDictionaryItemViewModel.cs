using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class SeriesDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Guid _manufacturerId;

    [ObservableProperty]
    private string _manufacturerName = string.Empty;

    [ObservableProperty]
    private double _hardwareAxisMm;

    public string HardwareAxisDisplay => $"{HardwareAxisMm:N1} мм";

    partial void OnHardwareAxisMmChanged(double value) => OnPropertyChanged(nameof(HardwareAxisDisplay));

    public static SeriesDictionaryItemViewModel FromModel(ProfileSeriesItem item, string manufacturerName) => new()
    {
        Id = item.Id,
        Name = item.Name,
        ManufacturerId = item.ManufacturerId,
        ManufacturerName = manufacturerName,
        HardwareAxisMm = item.HardwareAxisMm
    };

    public ProfileSeriesItem ToModel() => new()
    {
        Id = Id,
        Name = Name,
        ManufacturerId = ManufacturerId,
        HardwareAxisMm = HardwareAxisMm
    };

    public void RefreshFrom(ProfileSeriesItem item, string manufacturerName)
    {
        Name = item.Name;
        ManufacturerId = item.ManufacturerId;
        ManufacturerName = manufacturerName;
        HardwareAxisMm = item.HardwareAxisMm;
    }
}
