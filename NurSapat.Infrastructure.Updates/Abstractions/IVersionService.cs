using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IVersionService
{
    Version GetCurrentVersion();

    string GetCurrentVersionDisplay();

    string GetBuildVersion();

    bool IsNewer(Version candidate, Version? baseline = null);

    Version ParseVersion(string raw);
}
