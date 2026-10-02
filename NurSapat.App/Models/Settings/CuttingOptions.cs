namespace NurSapat.App.Models.Settings;

public enum ProfileCutLevel
{
    Simple,
    Complex
}

public enum ProfileWasteMode
{
    DoNotUse,
    OffcutsFirstThenWhole
}

public enum GlassWasteMode
{
    DoNotUse,
    OffcutsFirstThenSheets
}

public static class CuttingOptionExtensions
{
    public static string ToDisplayName(this ProfileCutLevel level) => level switch
    {
        ProfileCutLevel.Simple => "Простой (быстрый)",
        ProfileCutLevel.Complex => "Сложный (медленный)",
        _ => level.ToString()
    };

    public static string ToDisplayName(this ProfileWasteMode mode) => mode switch
    {
        ProfileWasteMode.DoNotUse => "Не учитывать",
        ProfileWasteMode.OffcutsFirstThenWhole => "Сначала обрезки, затем целые",
        _ => mode.ToString()
    };

    public static string ToDisplayName(this GlassWasteMode mode) => mode switch
    {
        GlassWasteMode.DoNotUse => "Не учитывать",
        GlassWasteMode.OffcutsFirstThenSheets => "Сначала обрезки, затем целые листы",
        _ => mode.ToString()
    };

    public static IReadOnlyList<ProfileCutLevelOption> ProfileCutLevelOptions { get; } =
        Enum.GetValues<ProfileCutLevel>()
            .Select(level => new ProfileCutLevelOption(level, level.ToDisplayName()))
            .ToArray();

    public static IReadOnlyList<ProfileWasteModeOption> ProfileWasteModeOptions { get; } =
        Enum.GetValues<ProfileWasteMode>()
            .Select(mode => new ProfileWasteModeOption(mode, mode.ToDisplayName()))
            .ToArray();

    public static IReadOnlyList<GlassWasteModeOption> GlassWasteModeOptions { get; } =
        Enum.GetValues<GlassWasteMode>()
            .Select(mode => new GlassWasteModeOption(mode, mode.ToDisplayName()))
            .ToArray();
}

public sealed record ProfileCutLevelOption(ProfileCutLevel Value, string DisplayName);

public sealed record ProfileWasteModeOption(ProfileWasteMode Value, string DisplayName);

public sealed record GlassWasteModeOption(GlassWasteMode Value, string DisplayName);
