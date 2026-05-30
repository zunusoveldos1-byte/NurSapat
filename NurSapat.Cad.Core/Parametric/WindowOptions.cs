namespace NurSapat.Cad.Core.Parametric;

public sealed class WindowOptions
{
    public double Width { get; set; } = 1300;

    public double Height { get; set; } = 1400;

    public double FrameProfileWidth { get; set; } = 72;

    public double ImpostProfileWidth { get; set; } = 60;

    public double SashProfileWidth { get; set; } = 58;

    public double SashOverlap { get; set; } = 8;

    public double GlassGap { get; set; } = 5;

    public ProfileKind ProfileKind { get; set; } = ProfileKind.Pvc;

    public string ProfileSystem { get; set; } = "Default PVC 72";

    public string LaminationColor { get; set; } = "Белый";
}
