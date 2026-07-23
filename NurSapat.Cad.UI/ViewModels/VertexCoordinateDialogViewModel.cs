using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.Helpers;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class VertexCoordinateDialogViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    private string _title = "Координаты вершины";

    [ObservableProperty]
    private string _coordinateXText = "0";

    [ObservableProperty]
    private string _coordinateYText = "0";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public double ResultX { get; private set; }

    public double ResultY { get; private set; }

    public VertexCoordinateDialogViewModel(double x, double y, string? title = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        CoordinateXText = SignedDecimalInput.FormatCoordinate(x);
        CoordinateYText = SignedDecimalInput.FormatCoordinate(y);
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

        ResultX = x;
        ResultY = y;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
