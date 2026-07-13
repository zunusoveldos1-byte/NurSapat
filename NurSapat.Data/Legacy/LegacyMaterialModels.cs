namespace NurSapat.Data.Legacy;

/// <summary>Legacy PostgreSQL enum <c>material_category</c> for commercial DB (Dapper/Npgsql).</summary>
public enum LegacyMaterialCategory
{
    Profile,
    Hardware,
    Glazing,
    Sealant,
    Reinforcement,
    Accessory,
    Service
}

/// <summary>Legacy PostgreSQL DTO for table <c>materials</c> in commercial DB.</summary>
public sealed class LegacyMaterial
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Article { get; set; } = "";

    public string Name { get; set; } = "";

    public LegacyMaterialCategory Category { get; set; }

    public string UnitOfMeasure { get; set; } = "м/п";

    public decimal Price { get; set; }

    public Guid? ColorId { get; set; }
}
