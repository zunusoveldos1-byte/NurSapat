namespace NurSapat.App.Models.Dictionaries;

public sealed class ManufacturerItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public double FalsCorrectionMm { get; set; }

    public double EndCuttingMm { get; set; }

    public double BladeWidthMm { get; set; } = 3.0;

    public double WeldingShrinkageMm { get; set; }

    public ManufacturerItem Clone() => new()
    {
        Id = Id,
        Name = Name,
        IsActive = IsActive,
        FalsCorrectionMm = FalsCorrectionMm,
        EndCuttingMm = EndCuttingMm,
        BladeWidthMm = BladeWidthMm,
        WeldingShrinkageMm = WeldingShrinkageMm
    };
}
