namespace NurSapat.App.Models.EditDialogs;

public sealed class CopyDrawingParameters
{
    public DrawingExportFormat Format { get; set; } = DrawingExportFormat.Bmp;

    public bool IncludeDimensionLines { get; set; } = true;

    public bool IncludeAxes { get; set; } = true;

    public bool IncludeLabels { get; set; } = true;

    public bool IncludeDimensions { get; set; } = true;

    public bool IncludeSashes { get; set; } = true;

    public bool IncludeImposts { get; set; } = true;

    public bool IncludeGlazing { get; set; } = true;

    public DrawingBackgroundMode Background { get; set; } = DrawingBackgroundMode.White;

    public int ScalePercent { get; set; } = 100;

    public int ResolutionDpi { get; set; } = 96;

    public CopyDrawingParameters Clone() => new()
    {
        Format = Format,
        IncludeDimensionLines = IncludeDimensionLines,
        IncludeAxes = IncludeAxes,
        IncludeLabels = IncludeLabels,
        IncludeDimensions = IncludeDimensions,
        IncludeSashes = IncludeSashes,
        IncludeImposts = IncludeImposts,
        IncludeGlazing = IncludeGlazing,
        Background = Background,
        ScalePercent = ScalePercent,
        ResolutionDpi = ResolutionDpi
    };
}
