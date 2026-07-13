namespace NurSapat.App.Models.Help;

public sealed class VersionInfo
{
    public string Version { get; init; } = string.Empty;

    public string ReleaseDate { get; init; } = string.Empty;

    public IReadOnlyList<string> Changes { get; init; } = [];
}

public sealed class ChangelogDocument
{
    public IReadOnlyList<VersionInfo> Versions { get; init; } = [];
}
