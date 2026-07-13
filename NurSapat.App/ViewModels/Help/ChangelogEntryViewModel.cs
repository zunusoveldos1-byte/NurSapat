namespace NurSapat.App.ViewModels.Help;

public sealed class ChangelogEntryViewModel
{
    public string Version { get; init; } = string.Empty;

    public string ReleaseDate { get; init; } = string.Empty;

    public string VersionLabel { get; init; } = string.Empty;

    public IReadOnlyList<string> Changes { get; init; } = [];
}
