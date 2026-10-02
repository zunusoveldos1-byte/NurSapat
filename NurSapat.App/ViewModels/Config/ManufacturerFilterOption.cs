namespace NurSapat.App.ViewModels.Config;

public sealed class ManufacturerFilterOption
{
    public Guid? ManufacturerId { get; init; }

    public string Name { get; init; } = string.Empty;

    public static ManufacturerFilterOption All { get; } = new()
    {
        ManufacturerId = null,
        Name = "Все"
    };

    public override string ToString() => Name;
}
