namespace NurSapat.App.Models.EditDialogs;

public static class StandardColorCatalog
{
    public static IReadOnlyList<StandardColorOption> Colors { get; } =
    [
        new() { Name = "Белый", Hex = "#FFFFFF" },
        new() { Name = "Коричневый", Hex = "#8B4513" },
        new() { Name = "Антрацит", Hex = "#383E42" },
        new() { Name = "Черный", Hex = "#1A1A1A" },
        new() { Name = "Орех", Hex = "#7B5B3E" },
        new() { Name = "Золотой дуб", Hex = "#C8A951" },
        new() { Name = "Махагон", Hex = "#6B2E1F" },
        new() { Name = "Любой цвет...", Hex = "#808080", IsCustomPicker = true }
    ];

    public static string? FindNameByHex(string hex)
    {
        var normalized = NormalizeHex(hex);
        return Colors.FirstOrDefault(c => !c.IsCustomPicker && NormalizeHex(c.Hex) == normalized)?.Name;
    }

    public static string NormalizeHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return "#FFFFFF";
        }

        var value = hex.Trim().ToUpperInvariant();
        if (!value.StartsWith('#'))
        {
            value = "#" + value;
        }

        return value.Length == 7 ? value : "#FFFFFF";
    }
}
