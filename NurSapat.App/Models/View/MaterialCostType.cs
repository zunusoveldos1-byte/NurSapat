namespace NurSapat.App.Models.View;

public enum MaterialCostType
{
    Profile,
    Glass,
    Hardware,
    Consumables
}

public static class MaterialCostTypeExtensions
{
    public static string ToDisplayName(this MaterialCostType type) => type switch
    {
        MaterialCostType.Profile => "Профиль",
        MaterialCostType.Glass => "Стекло",
        MaterialCostType.Hardware => "Фурнитура",
        MaterialCostType.Consumables => "Расходники",
        _ => type.ToString()
    };
}
