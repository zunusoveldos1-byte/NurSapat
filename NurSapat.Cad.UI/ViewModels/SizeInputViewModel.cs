using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Helpers;
using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class SizeInputViewModel : ObservableObject
{
    private static readonly PlacementAnchorOption[] AnchorOptions =
    [
        new() { Anchor = PlacementAnchor.BottomLeft, Label = "Левый нижний" },
        new() { Anchor = PlacementAnchor.TopLeft, Label = "Левый верхний" },
        new() { Anchor = PlacementAnchor.BottomRight, Label = "Правый нижний" },
        new() { Anchor = PlacementAnchor.TopRight, Label = "Правый верхний" }
    ];

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

    [ObservableProperty]
    private bool _showAnchorOptions;

    [ObservableProperty]
    private PlacementAnchorOption _selectedAnchorOption = AnchorOptions[0];

    public IReadOnlyList<PlacementAnchorOption> AnchorOptionList => AnchorOptions;

    public double ResultX { get; private set; }

    public double ResultY { get; private set; }

    public double ResultWidth { get; private set; }

    public double ResultHeight { get; private set; }

    public PlacementAnchor ResultAnchor { get; private set; } = PlacementAnchor.BottomLeft;

    public bool IsConfirmed { get; private set; }

    public SizeInputViewModel(double width, double height, double coordinateX, double coordinateY)
    {
        WidthText = SignedDecimalInput.FormatCoordinate(width);
        HeightText = SignedDecimalInput.FormatCoordinate(height);
        CoordinateXText = SignedDecimalInput.FormatCoordinate(coordinateX);
        CoordinateYText = SignedDecimalInput.FormatCoordinate(coordinateY);
    }

    public SizeInputViewModel(NewConstructionOptions options)
    {
        ShowAnchorOptions = true;
        WidthText = SignedDecimalInput.FormatCoordinate(options.Width);
        HeightText = SignedDecimalInput.FormatCoordinate(options.Height);
        CoordinateXText = SignedDecimalInput.FormatCoordinate(options.AnchorX);
        CoordinateYText = SignedDecimalInput.FormatCoordinate(options.AnchorY);
        SelectedAnchorOption = AnchorOptions.First(option => option.Anchor == options.Anchor);
    }

    partial void OnSelectedAnchorOptionChanged(PlacementAnchorOption value) =>
        ResultAnchor = value.Anchor;

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
        ResultAnchor = SelectedAnchorOption.Anchor;
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
