using NurSapat.App.Models.Help;

namespace NurSapat.App.Services.Help;

public interface IChangelogService
{
    IReadOnlyList<VersionInfo> LoadVersions();
}
