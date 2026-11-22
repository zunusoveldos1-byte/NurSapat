using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class DecorativeSpokeDialogViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    public IReadOnlyList<DecorativeSpokeType> AvailableSpokeTypes { get; } =
        Enum.GetValues<DecorativeSpokeType>().ToList();

    public IReadOnlyList<double> AvailableThicknesses { get; } =
        DecorativeSpokeElement.StandardThicknessesMm;

    public IReadOnlyList<string> AvailableColors { get; } =
    [
        DecorativeSpokeElement.Colors.White,
        DecorativeSpokeElement.Colors.Gold,
        DecorativeSpokeElement.Colors.Brown
    ];

    [ObservableProperty]
    private DecorativeSpokeType _selectedSpokeType = DecorativeSpokeType.StandardGrid;

    [ObservableProperty]
    private double _selectedThickness = 18;

    [ObservableProperty]
    private string _selectedColor = DecorativeSpokeElement.Colors.White;

    [ObservableProperty]
    private string _rowsText = "2";

    [ObservableProperty]
    private string _columnsText = "2";

    [ObservableProperty]
    private string _raysText = "4";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isArchedGlazing;

    public bool IsGridVisible => SelectedSpokeType == DecorativeSpokeType.StandardGrid;

    public bool IsRayCountVisible =>
        SelectedSpokeType is DecorativeSpokeType.Sunburst;

    public bool IsArchHintVisible =>
        SelectedSpokeType is DecorativeSpokeType.Sunburst or DecorativeSpokeType.ArchContour;

    public DecorativeSpokeType ResultSpokeType { get; private set; }

    public double ResultThickness { get; private set; } = 18;

    public string ResultColor { get; private set; } = DecorativeSpokeElement.Colors.White;

    public int ResultRowsCount { get; private set; } = 2;

    public int ResultColumnsCount { get; private set; } = 2;

    public int ResultRayCount { get; private set; } = 4;

    public DecorativeSpokeDialogViewModel(bool isArchedGlazing)
    {
        IsArchedGlazing = isArchedGlazing;
        if (isArchedGlazing)
        {
            SelectedSpokeType = DecorativeSpokeType.Sunburst;
        }
    }

    partial void OnSelectedSpokeTypeChanged(DecorativeSpokeType value)
    {
        OnPropertyChanged(nameof(IsGridVisible));
        OnPropertyChanged(nameof(IsRayCountVisible));
        OnPropertyChanged(nameof(IsArchHintVisible));
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;

        if (!int.TryParse(RowsText.Trim(), out var rows) || rows < 1)
        {
            ValidationMessage = "Количество строк должно быть целым числом ≥ 1";
            return;
        }

        if (!int.TryParse(ColumnsText.Trim(), out var columns) || columns < 1)
        {
            ValidationMessage = "Количество столбцов должно быть целым числом ≥ 1";
            return;
        }

        if (!int.TryParse(RaysText.Trim(), out var rays) || rays < 2)
        {
            ValidationMessage = "Количество лучей должно быть целым числом ≥ 2";
            return;
        }

        ResultSpokeType = SelectedSpokeType;
        ResultThickness = SelectedThickness;
        ResultColor = SelectedColor;
        ResultRowsCount = rows;
        ResultColumnsCount = columns;
        ResultRayCount = rays;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
