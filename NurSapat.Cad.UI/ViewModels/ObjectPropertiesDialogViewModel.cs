using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.Helpers;
using NurSapat.Cad.UI.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed record ObjectPropertiesDialogResult(
    string Label,
    double X,
    double Y,
    double Width,
    double Height,
    Guid? MaterialId,
    Guid? ProfileSeriesId,
    SashOpeningType? OpeningType,
    SashHingeSide? HingeSide,
    bool HasHardware,
    bool HasHandle,
    bool HasMosquitoNet,
    GlazingType? FillType,
    double? AxisPosition,
    double? ExtremeOffsetMm,
    ImpostEdgeAlignment? EdgeAlignment,
    double? ArcHeightMm,
    double? SillThicknessMm,
    double? SillWidthMm,
    double? SillLeftOverhangMm,
    double? SillRightOverhangMm);

public sealed partial class ObjectPropertiesDialogViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    public string ObjectKind { get; }
    public CadSelectableKind SelectableKind { get; }
    public bool ShowSizeFields { get; }
    public bool ShowPositionFields { get; }
    public bool ShowCatalogFields { get; }
    public bool ShowSashFields { get; }
    public bool ShowGlazingFields { get; }
    public bool ShowImpostAxisField { get; }
    public bool ShowExtremeFields { get; }
    public bool ShowArcField { get; }
    public bool ShowSillFields { get; }
    public string SizeSummary { get; }

    public IReadOnlyList<NamedGuidOption> MaterialOptions { get; }
    public IReadOnlyList<NamedGuidOption> SeriesOptions { get; }
    public IReadOnlyList<SashOpeningType> OpeningTypeOptions { get; } = Enum.GetValues<SashOpeningType>();
    public IReadOnlyList<SashHingeSide> HingeSideOptions { get; } = Enum.GetValues<SashHingeSide>();
    public IReadOnlyList<GlazingType> FillTypeOptions { get; } = Enum.GetValues<GlazingType>();
    public IReadOnlyList<ImpostEdgeAlignment> EdgeAlignmentOptions { get; } = Enum.GetValues<ImpostEdgeAlignment>();

    [ObservableProperty] private string _labelText = string.Empty;
    [ObservableProperty] private string _xText = "0";
    [ObservableProperty] private string _yText = "0";
    [ObservableProperty] private string _widthText = "0";
    [ObservableProperty] private string _heightText = "0";
    [ObservableProperty] private NamedGuidOption? _selectedMaterial;
    [ObservableProperty] private NamedGuidOption? _selectedSeries;
    [ObservableProperty] private SashOpeningType? _selectedOpeningType;
    [ObservableProperty] private SashHingeSide? _selectedHingeSide;
    [ObservableProperty] private bool _hasHardware;
    [ObservableProperty] private bool _hasHandle;
    [ObservableProperty] private bool _hasMosquitoNet;
    [ObservableProperty] private GlazingType? _selectedFillType;
    [ObservableProperty] private string _axisPositionText = "0";
    [ObservableProperty] private string _extremeOffsetText = "0";
    [ObservableProperty] private ImpostEdgeAlignment? _selectedEdgeAlignment;
    [ObservableProperty] private string _arcHeightText = "0";
    [ObservableProperty] private string _sillThicknessText = "0";
    [ObservableProperty] private string _sillWidthText = "0";
    [ObservableProperty] private string _sillLeftOverhangText = "0";
    [ObservableProperty] private string _sillRightOverhangText = "0";
    [ObservableProperty] private string _validationMessage = string.Empty;

    public ObjectPropertiesDialogResult? Result { get; private set; }

    public ObjectPropertiesDialogViewModel(ObjectPropertiesDialogModel model)
    {
        ObjectKind = model.ObjectKind;
        SelectableKind = model.SelectableKind;
        ShowSizeFields = model.ShowSizeFields;
        ShowPositionFields = model.ShowPositionFields;
        ShowCatalogFields = model.ShowCatalogFields;
        ShowSashFields = model.ShowSashFields;
        ShowGlazingFields = model.ShowGlazingFields;
        ShowImpostAxisField = model.ShowImpostAxisField;
        ShowExtremeFields = model.ShowExtremeFields;
        ShowArcField = model.ShowArcField;
        ShowSillFields = model.ShowSillFields;
        SizeSummary = model.SizeSummary;
        MaterialOptions = model.MaterialOptions;
        SeriesOptions = model.SeriesOptions;

        LabelText = model.Label;
        XText = SignedDecimalInput.FormatCoordinate(model.X);
        YText = SignedDecimalInput.FormatCoordinate(model.Y);
        WidthText = SignedDecimalInput.FormatCoordinate(model.Width);
        HeightText = SignedDecimalInput.FormatCoordinate(model.Height);
        SelectedMaterial = MaterialOptions.FirstOrDefault(o => o.Id == model.MaterialId) ?? MaterialOptions.FirstOrDefault();
        SelectedSeries = SeriesOptions.FirstOrDefault(o => o.Id == model.ProfileSeriesId) ?? SeriesOptions.FirstOrDefault();
        SelectedOpeningType = model.OpeningType;
        SelectedHingeSide = model.HingeSide;
        HasHardware = model.HasHardware;
        HasHandle = model.HasHandle;
        HasMosquitoNet = model.HasMosquitoNet;
        SelectedFillType = model.FillType;
        AxisPositionText = SignedDecimalInput.FormatCoordinate(model.AxisPosition ?? 0);
        ExtremeOffsetText = SignedDecimalInput.FormatCoordinate(model.ExtremeOffsetMm ?? 0);
        SelectedEdgeAlignment = model.EdgeAlignment;
        ArcHeightText = SignedDecimalInput.FormatCoordinate(model.ArcHeightMm ?? 0);
        SillThicknessText = SignedDecimalInput.FormatCoordinate(model.SillThicknessMm ?? 0);
        SillWidthText = SignedDecimalInput.FormatCoordinate(model.SillWidthMm ?? 0);
        SillLeftOverhangText = SignedDecimalInput.FormatCoordinate(model.SillLeftOverhangMm ?? 0);
        SillRightOverhangText = SignedDecimalInput.FormatCoordinate(model.SillRightOverhangMm ?? 0);
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;

        if (!SignedDecimalInput.TryParseCoordinate(XText, out var x)
            || !SignedDecimalInput.TryParseCoordinate(YText, out var y))
        {
            ValidationMessage = "Некорректные координаты";
            return;
        }

        double width = 0, height = 0;
        if (ShowSizeFields)
        {
            if (!SignedDecimalInput.TryParsePositiveDimension(WidthText, out width)
                || !SignedDecimalInput.TryParsePositiveDimension(HeightText, out height))
            {
                ValidationMessage = "Ширина и высота должны быть больше 0";
                return;
            }
        }

        double? axis = null;
        if (ShowImpostAxisField)
        {
            if (!SignedDecimalInput.TryParseCoordinate(AxisPositionText, out var axisValue))
            {
                ValidationMessage = "Некорректная координата оси импоста";
                return;
            }

            axis = axisValue;
        }

        double? extremeOffset = null;
        if (ShowExtremeFields)
        {
            if (!SignedDecimalInput.TryParseCoordinate(ExtremeOffsetText, out var offset) || offset < 0)
            {
                ValidationMessage = "Смещение крайнего импоста должно быть ≥ 0";
                return;
            }

            extremeOffset = offset;
        }

        double? arcHeight = null;
        if (ShowArcField)
        {
            if (!SignedDecimalInput.TryParseCoordinate(ArcHeightText, out var heightValue) || heightValue < 0)
            {
                ValidationMessage = "Стрела прогиба должна быть ≥ 0";
                return;
            }

            arcHeight = heightValue;
        }

        double? sillThickness = null, sillWidth = null, sillLeft = null, sillRight = null;
        if (ShowSillFields)
        {
            if (!SignedDecimalInput.TryParsePositiveDimension(SillThicknessText, out var thickness)
                || !SignedDecimalInput.TryParsePositiveDimension(SillWidthText, out var sillW)
                || !SignedDecimalInput.TryParseCoordinate(SillLeftOverhangText, out var left)
                || !SignedDecimalInput.TryParseCoordinate(SillRightOverhangText, out var right)
                || left < 0
                || right < 0)
            {
                ValidationMessage = "Проверьте параметры подоконника";
                return;
            }

            sillThickness = thickness;
            sillWidth = sillW;
            sillLeft = left;
            sillRight = right;
        }

        Result = new ObjectPropertiesDialogResult(
            LabelText.Trim(),
            x,
            y,
            width,
            height,
            SelectedMaterial?.Id,
            SelectedSeries?.Id,
            SelectedOpeningType,
            SelectedHingeSide,
            HasHardware,
            HasHandle,
            HasMosquitoNet,
            SelectedFillType,
            axis,
            extremeOffset,
            SelectedEdgeAlignment,
            arcHeight,
            sillThickness,
            sillWidth,
            sillLeft,
            sillRight);
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}

public sealed record NamedGuidOption(Guid? Id, string Name);

public sealed record ObjectPropertiesDialogModel(
    string ObjectKind,
    CadSelectableKind SelectableKind,
    string Label,
    double X,
    double Y,
    double Width,
    double Height,
    string SizeSummary,
    bool ShowSizeFields,
    bool ShowPositionFields,
    bool ShowCatalogFields,
    bool ShowSashFields,
    bool ShowGlazingFields,
    bool ShowImpostAxisField,
    bool ShowExtremeFields,
    bool ShowArcField,
    bool ShowSillFields,
    Guid? MaterialId,
    Guid? ProfileSeriesId,
    IReadOnlyList<NamedGuidOption> MaterialOptions,
    IReadOnlyList<NamedGuidOption> SeriesOptions,
    SashOpeningType? OpeningType = null,
    SashHingeSide? HingeSide = null,
    bool HasHardware = false,
    bool HasHandle = false,
    bool HasMosquitoNet = false,
    GlazingType? FillType = null,
    double? AxisPosition = null,
    double? ExtremeOffsetMm = null,
    ImpostEdgeAlignment? EdgeAlignment = null,
    double? ArcHeightMm = null,
    double? SillThicknessMm = null,
    double? SillWidthMm = null,
    double? SillLeftOverhangMm = null,
    double? SillRightOverhangMm = null);
