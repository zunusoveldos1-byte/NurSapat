using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.ViewModels;

public sealed partial class PrintSettingsViewModel : ObservableObject
{
    [ObservableProperty] private double _scalePercent = 100;
    [ObservableProperty] private bool _drawBorder = true;
    [ObservableProperty] private bool _drawStamp = true;
    [ObservableProperty] private bool _drawCompanyLogo;
    [ObservableProperty] private bool _drawSignature;
    [ObservableProperty] private double _marginLeftMm = 10;
    [ObservableProperty] private double _marginRightMm = 10;
    [ObservableProperty] private double _marginTopMm = 10;
    [ObservableProperty] private double _marginBottomMm = 10;
    [ObservableProperty] private MeasurementUnit _units = MeasurementUnit.Millimeters;
    [ObservableProperty] private double _thinLineWidthMm = 0.18;
    [ObservableProperty] private double _mediumLineWidthMm = 0.35;
    [ObservableProperty] private double _thickLineWidthMm = 0.7;
    [ObservableProperty] private string _dimensionFontFamily = "Inter";
    [ObservableProperty] private double _dimensionFontSizePt = 8;
    [ObservableProperty] private bool _showDimensions = true;
    [ObservableProperty] private PaperFormat _paperFormat = PaperFormat.A4;
    [ObservableProperty] private PageOrientation _orientation = PageOrientation.Landscape;

    public event EventHandler<bool>? CloseRequested;

    public PrintSettingsViewModel(PrintSettings settings)
    {
        ScalePercent = settings.ScalePercent;
        DrawBorder = settings.DrawBorder;
        DrawStamp = settings.DrawStamp;
        DrawCompanyLogo = settings.DrawCompanyLogo;
        DrawSignature = settings.DrawSignature;
        MarginLeftMm = settings.MarginLeftMm;
        MarginRightMm = settings.MarginRightMm;
        MarginTopMm = settings.MarginTopMm;
        MarginBottomMm = settings.MarginBottomMm;
        Units = settings.Units;
        ThinLineWidthMm = settings.ThinLineWidthMm;
        MediumLineWidthMm = settings.MediumLineWidthMm;
        ThickLineWidthMm = settings.ThickLineWidthMm;
        DimensionFontFamily = settings.DimensionFontFamily;
        DimensionFontSizePt = settings.DimensionFontSizePt;
        ShowDimensions = settings.ShowDimensions;
        PaperFormat = settings.PaperFormat;
        Orientation = settings.Orientation;
    }

    [RelayCommand]
    private void Save() => CloseRequested?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    public PrintSettings ToSettings() => new()
    {
        ScalePercent = ScalePercent,
        DrawBorder = DrawBorder,
        DrawStamp = DrawStamp,
        DrawCompanyLogo = DrawCompanyLogo,
        DrawSignature = DrawSignature,
        MarginLeftMm = MarginLeftMm,
        MarginRightMm = MarginRightMm,
        MarginTopMm = MarginTopMm,
        MarginBottomMm = MarginBottomMm,
        Units = Units,
        ThinLineWidthMm = ThinLineWidthMm,
        MediumLineWidthMm = MediumLineWidthMm,
        ThickLineWidthMm = ThickLineWidthMm,
        DimensionFontFamily = DimensionFontFamily,
        DimensionFontSizePt = DimensionFontSizePt,
        ShowDimensions = ShowDimensions,
        PaperFormat = PaperFormat,
        Orientation = Orientation
    };
}
