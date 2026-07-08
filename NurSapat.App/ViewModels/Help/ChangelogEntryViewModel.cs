namespace NurSapat.App.ViewModels.Help;

public sealed class ChangelogEntryViewModel
{
    public string Version { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<string> Items { get; init; } = [];
}
