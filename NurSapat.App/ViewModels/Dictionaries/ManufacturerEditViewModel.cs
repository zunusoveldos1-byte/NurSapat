using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ManufacturerEditViewModel : ObservableObject
{
    private Guid _id;

    public string WindowTitle => "Редактирование производителя";

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

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(ManufacturerItem source)
    {
        _id = source.Id;
        Name = source.Name;
        IsActive = source.IsActive;
        FalsCorrectionMm = source.FalsCorrectionMm;
        EndCuttingMm = source.EndCuttingMm;
        BladeWidthMm = source.BladeWidthMm;
        WeldingShrinkageMm = source.WeldingShrinkageMm;
        ValidationMessage = string.Empty;
    }

    public ManufacturerItem BuildResult() => new()
    {
        Id = _id,
        Name = Name,
        IsActive = IsActive,
        FalsCorrectionMm = FalsCorrectionMm,
        EndCuttingMm = EndCuttingMm,
        BladeWidthMm = BladeWidthMm,
        WeldingShrinkageMm = WeldingShrinkageMm
    };

    [RelayCommand]
    private void Save()
    {
        ValidationMessage = Validate();
        if (!string.IsNullOrEmpty(ValidationMessage))
        {
            return;
        }

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    private static string Validate() => string.Empty;
}
