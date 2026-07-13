using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.Helpers;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class RadiusInputViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    private string _radiusText = "500";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public double ResultRadius { get; private set; }

    public RadiusInputViewModel(double initialRadius)
    {
        RadiusText = SignedDecimalInput.FormatCoordinate(initialRadius);
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;
        if (!SignedDecimalInput.TryParsePositiveDimension(RadiusText, out var radius))
        {
            ValidationMessage = "Радиус должен быть больше 0";
            return;
        }

        ResultRadius = radius;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
