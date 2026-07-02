namespace NurSapat.App.ViewModels.Dictionaries;

public sealed class ManufacturerSelectionOption
{
    public Guid? ManufacturerId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public static ManufacturerSelectionOption All { get; } = new()
    {
        DisplayName = "Все"
    };

    public static ManufacturerSelectionOption FromManufacturer(Guid id, string name) => new()
    {
        ManufacturerId = id,
        DisplayName = name
    };
}
