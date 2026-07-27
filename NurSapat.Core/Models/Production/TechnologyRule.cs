namespace NurSapat.Core.Models.Production;

public sealed class TechnologyRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProfileSystemCode { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public decimal MinRadiusMm { get; set; }
    public decimal MaxSashWidthMm { get; set; }
    public decimal MaxSashHeightMm { get; set; }
    public decimal MaxSashWeightKg { get; set; }
    public decimal MaxInertiaIx { get; set; }
}
