namespace NurSapat.Cad.Core.Parametric;

public sealed class SectionFill
{
    public SectionFillKind Kind { get; set; } = SectionFillKind.Empty;

    public string ProfileSystem { get; set; } = "Default PVC 72";

    public string LaminationColor { get; set; } = "Белый";

    public string OpeningType { get; set; } = "Поворотно-откидная";

    public string HardwareBrand { get; set; } = "Roto";

    public string GlassFormula { get; set; } = "4*16*4 Простой";

    public List<SashHardwareModifier> HardwareModifiers { get; } = new();

    public bool HasSash => Kind == SectionFillKind.Sash;
}
