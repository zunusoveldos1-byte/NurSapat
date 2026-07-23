namespace NurSapat.Core.Models.Project;

public enum PaperFormat
{
    A4,
    A3,
    A2,
    A1,
    A0
}

public enum PageOrientation
{
    Portrait,
    Landscape
}

public enum PrintColorMode
{
    Color,
    BlackAndWhite
}

public enum MeasurementUnit
{
    Millimeters,
    Centimeters,
    Meters
}

/// <summary>
/// Параметры печати, хранящиеся внутри проекта.
/// </summary>
public sealed class PrintSettings
{
    public double ScalePercent { get; set; } = 100;

    public bool DrawBorder { get; set; } = true;

    public bool DrawStamp { get; set; } = true;

    public bool DrawCompanyLogo { get; set; }

    public bool DrawSignature { get; set; }

    public double MarginLeftMm { get; set; } = 10;

    public double MarginRightMm { get; set; } = 10;

    public double MarginTopMm { get; set; } = 10;

    public double MarginBottomMm { get; set; } = 10;

    public MeasurementUnit Units { get; set; } = MeasurementUnit.Millimeters;

    public double ThinLineWidthMm { get; set; } = 0.18;

    public double MediumLineWidthMm { get; set; } = 0.35;

    public double ThickLineWidthMm { get; set; } = 0.7;

    public string DimensionFontFamily { get; set; } = "Inter";

    public double DimensionFontSizePt { get; set; } = 8;

    public bool ShowDimensions { get; set; } = true;

    public PaperFormat PaperFormat { get; set; } = PaperFormat.A4;

    public PageOrientation Orientation { get; set; } = PageOrientation.Landscape;

    public PrintColorMode ColorMode { get; set; } = PrintColorMode.Color;

    public bool PrintDimensions { get; set; } = true;

    public bool PrintCoordinates { get; set; }

    public PrintSettings Clone() => new()
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
        Orientation = Orientation,
        ColorMode = ColorMode,
        PrintDimensions = PrintDimensions,
        PrintCoordinates = PrintCoordinates
    };
}
