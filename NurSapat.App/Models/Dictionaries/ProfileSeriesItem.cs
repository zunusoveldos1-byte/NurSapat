namespace NurSapat.App.Models.Dictionaries;

public sealed class ProfileSeriesItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid ManufacturerId { get; set; }

    public double HardwareAxisMm { get; set; } = 13.0;

    public ProfileSeriesItem Clone() => new()
    {
        Id = Id,
        Name = Name,
        ManufacturerId = ManufacturerId,
        HardwareAxisMm = HardwareAxisMm
    };
}
