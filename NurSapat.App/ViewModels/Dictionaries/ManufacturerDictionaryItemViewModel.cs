using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ManufacturerDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private double _falsCorrectionMm;

    [ObservableProperty]
    private double _endCuttingMm;

    [ObservableProperty]
    private double _bladeWidthMm;

    [ObservableProperty]
    private double _weldingShrinkageMm;

    public string IsActiveDisplay => IsActive ? "Да" : "Нет";

    public string EndCuttingDisplay => $"{EndCuttingMm:N2}";

    public string BladeWidthDisplay => $"{BladeWidthMm:N2}";

    public string WeldingShrinkageDisplay => $"{WeldingShrinkageMm:N2}";

    partial void OnIsActiveChanged(bool value) => OnPropertyChanged(nameof(IsActiveDisplay));

    partial void OnEndCuttingMmChanged(double value) => OnPropertyChanged(nameof(EndCuttingDisplay));

    partial void OnBladeWidthMmChanged(double value) => OnPropertyChanged(nameof(BladeWidthDisplay));

    partial void OnWeldingShrinkageMmChanged(double value) => OnPropertyChanged(nameof(WeldingShrinkageDisplay));

    public static ManufacturerDictionaryItemViewModel FromModel(ManufacturerItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        IsActive = item.IsActive,
        FalsCorrectionMm = item.FalsCorrectionMm,
        EndCuttingMm = item.EndCuttingMm,
        BladeWidthMm = item.BladeWidthMm,
        WeldingShrinkageMm = item.WeldingShrinkageMm
    };

    public ManufacturerItem ToModel() => new()
    {
        Id = Id,
        Name = Name,
        IsActive = IsActive,
        FalsCorrectionMm = FalsCorrectionMm,
        EndCuttingMm = EndCuttingMm,
        BladeWidthMm = BladeWidthMm,
        WeldingShrinkageMm = WeldingShrinkageMm
    };

    public void RefreshFrom(ManufacturerItem item)
    {
        Name = item.Name;
        IsActive = item.IsActive;
        FalsCorrectionMm = item.FalsCorrectionMm;
        EndCuttingMm = item.EndCuttingMm;
        BladeWidthMm = item.BladeWidthMm;
        WeldingShrinkageMm = item.WeldingShrinkageMm;
    }
}
