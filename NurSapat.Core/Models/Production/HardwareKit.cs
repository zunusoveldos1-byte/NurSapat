namespace NurSapat.Core.Models.Production;

public sealed class HardwareKit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SystemBrand { get; set; } = string.Empty;
    public decimal MinSashWidth { get; set; }
    public decimal MaxSashWidth { get; set; }
    public decimal MinSashHeight { get; set; }
    public decimal MaxSashHeight { get; set; }
    public decimal MaxWeightKg { get; set; }
    public string ComponentsJson { get; set; } = "[]";
}
