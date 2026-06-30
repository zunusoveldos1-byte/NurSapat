using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.Helpers;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class SizeInputViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    private string _coordinateXText = "0.0";

    [ObservableProperty]
    private string _coordinateYText = "0.0";

    [ObservableProperty]
    private string _widthText = "1000";

    [ObservableProperty]
    private string _heightText = "1000";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public double ResultX { get; private set; }

    public double ResultY { get; private set; }

    public double ResultWidth { get; private set; }

    public double ResultHeight { get; private set; }

    public bool IsConfirmed { get; private set; }

    public SizeInputViewModel(double width, double height, double coordinateX, double coordinateY)
    {
        WidthText = SignedDecimalInput.FormatCoordinate(width);
        HeightText = SignedDecimalInput.FormatCoordinate(height);
        CoordinateXText = SignedDecimalInput.FormatCoordinate(coordinateX);
        CoordinateYText = SignedDecimalInput.FormatCoordinate(coordinateY);
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;

        if (!SignedDecimalInput.TryParseCoordinate(CoordinateXText, out var x))
        {
            ValidationMessage = "Некорректная координата X";
            return;
        }

        if (!SignedDecimalInput.TryParseCoordinate(CoordinateYText, out var y))
        {
            ValidationMessage = "Некорректная координата Y";
            return;
        }

        if (!SignedDecimalInput.TryParsePositiveDimension(WidthText, out var width))
        {
            ValidationMessage = "Ширина (L) должна быть больше 0";
            return;
        }

        if (!SignedDecimalInput.TryParsePositiveDimension(HeightText, out var height))
        {
            ValidationMessage = "Высота (H) должна быть больше 0";
            return;
        }

        ResultX = x;
        ResultY = y;
        ResultWidth = width;
        ResultHeight = height;
        IsConfirmed = true;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsConfirmed = false;
        CloseRequested?.Invoke(this, false);
    }
}
