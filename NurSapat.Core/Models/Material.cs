namespace NurSapat.Core.Models;

public enum MaterialCategory
{
    Profile,
    Hardware,
    Glazing,
    Sealant,
    Reinforcement,
    Accessory,
    Service
}

public class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Article { get; set; } = "";

    public string Name { get; set; } = "";

    public MaterialCategory Category { get; set; }

    public string UnitOfMeasure { get; set; } = "м/п";

    public decimal Price { get; set; }

    public Guid? ColorId { get; set; }
}
