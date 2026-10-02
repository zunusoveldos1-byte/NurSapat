namespace NurSapat.App.Models.Settings;

public enum VatMode
{
    DoNotUse,
    WithVatIncluded,
    WithoutVatSeparate
}

public static class VatModeExtensions
{
    public static string ToDisplayName(this VatMode mode) => mode switch
    {
        VatMode.DoNotUse => "не использовать",
        VatMode.WithVatIncluded => "суммы с НДС от общей суммы",
        VatMode.WithoutVatSeparate => "суммы без НДС отдельно",
        _ => mode.ToString()
    };

    public static IReadOnlyList<VatModeOption> AllOptions { get; } =
        Enum.GetValues<VatMode>()
            .Select(mode => new VatModeOption(mode, mode.ToDisplayName()))
            .ToArray();
}

public sealed record VatModeOption(VatMode Value, string DisplayName);
