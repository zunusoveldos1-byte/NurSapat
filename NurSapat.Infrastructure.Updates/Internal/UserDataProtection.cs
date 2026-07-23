namespace NurSapat.Infrastructure.Updates.Internal;

public static class UserDataProtection
{
    public static readonly HashSet<string> ProtectedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Data",
        "Orders",
        "Database",
        "Settings",
        "Licenses",
        "Logs",
        "Temp",
        "Cache",
        "Profiles",
        "Backup",
        "Updates"
    };

    public static bool IsProtectedDirectory(string directoryName) =>
        ProtectedDirectoryNames.Contains(directoryName);

    public static bool ShouldSkipPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var firstSegment = relativePath
            .Replace('\\', '/')
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return firstSegment is not null && IsProtectedDirectory(firstSegment);
    }
}
