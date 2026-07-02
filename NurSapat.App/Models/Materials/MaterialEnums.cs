namespace NurSapat.App.Models.Materials;

public enum MaterialSection
{
    Profile,
    Components,
    Sealants,
    Glazing,
    Hardware,
    Labor,
    MaterialWithoutMarkup,
    AdditionalMaterial
}

public enum MaterialMainUnit
{
    LinearMeter,
    SquareMeter,
    Piece,
    Kilogram,
    Set
}

public enum MaterialLinearGroup
{
    Profile,
    Reinforcement,
    Bead,
    Spacer
}

public enum MaterialSquareGroup
{
    Glass,
    Panel
}

public enum MaterialCurrency
{
    Kgs,
    Usd,
    Eur,
    Cny
}

public static class MaterialEnumLabels
{
    public static string Section(MaterialSection section) => section switch
    {
        MaterialSection.Profile => "Профиль",
        MaterialSection.Components => "Комплектующие",
        MaterialSection.Sealants => "Уплотнители",
        MaterialSection.Glazing => "Остекление",
        MaterialSection.Hardware => "Фурнитура",
        MaterialSection.Labor => "Трудозатраты",
        MaterialSection.MaterialWithoutMarkup => "Материал без удорожания",
        MaterialSection.AdditionalMaterial => "Дополнительный материал",
        _ => section.ToString()
    };

    public static string MainUnit(MaterialMainUnit unit) => unit switch
    {
        MaterialMainUnit.LinearMeter => "м/п",
        MaterialMainUnit.SquareMeter => "м.кв.",
        MaterialMainUnit.Piece => "шт.",
        MaterialMainUnit.Kilogram => "кг",
        MaterialMainUnit.Set => "компл.",
        _ => unit.ToString()
    };

    public static string LinearGroup(MaterialLinearGroup group) => group switch
    {
        MaterialLinearGroup.Profile => "Профиль",
        MaterialLinearGroup.Reinforcement => "Армирование",
        MaterialLinearGroup.Bead => "Штапик",
        MaterialLinearGroup.Spacer => "Дистанционер",
        _ => group.ToString()
    };

    public static string SquareGroup(MaterialSquareGroup group) => group switch
    {
        MaterialSquareGroup.Glass => "Стекло (стеклопакет)",
        MaterialSquareGroup.Panel => "Панель",
        _ => group.ToString()
    };

    public static string Currency(MaterialCurrency currency) => currency switch
    {
        MaterialCurrency.Kgs => "KGS",
        MaterialCurrency.Usd => "USD",
        MaterialCurrency.Eur => "EUR",
        MaterialCurrency.Cny => "CNY",
        _ => currency.ToString()
    };

    public static MaterialCurrency ParseCurrency(string code) => code.ToUpperInvariant() switch
    {
        "USD" => MaterialCurrency.Usd,
        "EUR" => MaterialCurrency.Eur,
        "CNY" => MaterialCurrency.Cny,
        _ => MaterialCurrency.Kgs
    };
}
