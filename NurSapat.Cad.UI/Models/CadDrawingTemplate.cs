namespace NurSapat.Cad.UI.Models;

public sealed class CadDrawingTemplate
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public bool IsFakeFrame { get; set; }

    public string? GeometryJson { get; set; }

    public bool HasFullGeometry => !string.IsNullOrWhiteSpace(GeometryJson);

    public bool IsBlank => !HasFullGeometry && (WidthMm <= 0 || HeightMm <= 0);
}
