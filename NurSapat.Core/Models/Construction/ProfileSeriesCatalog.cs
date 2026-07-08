namespace NurSapat.Core.Models.Construction;

/// <summary>Справочник серий профилей (демо-данные для CAD/ERP).</summary>
public static class ProfileSeriesCatalog
{
    public static readonly Guid Pvc60Id = Guid.Parse("a1000001-0000-4000-8000-000000000001");
    public static readonly Guid Pvc70Id = Guid.Parse("a1000002-0000-4000-8000-000000000002");
    public static readonly Guid AluWarm75Id = Guid.Parse("a1000003-0000-4000-8000-000000000003");

    public static IReadOnlyList<ProfileSeries> All { get; } =
    [
        new ProfileSeries { Id = Pvc60Id, Name = "ПВХ 60 мм", SystemDepth = 60.0 },
        new ProfileSeries { Id = Pvc70Id, Name = "ПВХ 70 мм", SystemDepth = 70.0 },
        new ProfileSeries { Id = AluWarm75Id, Name = "Алюминий Тёплый 75 мм", SystemDepth = 75.0 }
    ];

    public static ProfileSeries? FindById(Guid? id) =>
        id is Guid value ? All.FirstOrDefault(series => series.Id == value) : null;

    public static ProfileSeries? FindByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var key = NormalizeKey(name);
        return All.FirstOrDefault(series =>
            NormalizeKey(series.Name) == key
            || series.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeKey(string value) =>
        new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
}
