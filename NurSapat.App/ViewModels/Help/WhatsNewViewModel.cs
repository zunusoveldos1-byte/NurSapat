using NurSapat.App.Services.Help;

namespace NurSapat.App.ViewModels.Help;

public sealed class WhatsNewViewModel : HelpDialogViewModelBase
{
    public WhatsNewViewModel(IChangelogService changelogService)
    {
        ChangelogEntries = changelogService.LoadVersions()
            .Select(version => new ChangelogEntryViewModel
            {
                Version = version.Version,
                ReleaseDate = version.ReleaseDate,
                VersionLabel = $"Версия {version.Version}",
                Changes = version.Changes
            })
            .ToList();
    }

    public IReadOnlyList<ChangelogEntryViewModel> ChangelogEntries { get; }
}
