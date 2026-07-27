using System.Text.Json;
using NurSapat.Core.Settings;

namespace NurSapat.App.Services.Documents;

public interface IRecentFilesService
{
    IReadOnlyList<RecentFileEntry> GetRecentFiles();

    void AddRecentFile(string filePath);

    event EventHandler? RecentFilesChanged;
}

public sealed record RecentFileEntry(string FilePath, string DisplayName, DateTimeOffset LastOpened);

public sealed class RecentFilesService : IRecentFilesService
{
    private const int MaxRecentFiles = 10;

    private readonly string _recentFilePath;
    private readonly List<RecentFileEntry> _entries = [];

    public event EventHandler? RecentFilesChanged;

    public RecentFilesService(EnvironmentSettings environmentSettings)
    {
        _recentFilePath = Path.Combine(environmentSettings.AppDataPath, "recent-projects.json");
        Load();
    }

    public IReadOnlyList<RecentFileEntry> GetRecentFiles() => _entries.AsReadOnly();

    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var normalized = Path.GetFullPath(filePath);
        _entries.RemoveAll(entry => string.Equals(entry.FilePath, normalized, StringComparison.OrdinalIgnoreCase));

        _entries.Insert(0, new RecentFileEntry(
            normalized,
            Path.GetFileName(normalized),
            DateTimeOffset.UtcNow));

        if (_entries.Count > MaxRecentFiles)
        {
            _entries.RemoveRange(MaxRecentFiles, _entries.Count - MaxRecentFiles);
        }

        Save();
        RecentFilesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Load()
    {
        if (!File.Exists(_recentFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_recentFilePath);
            var items = JsonSerializer.Deserialize<List<RecentFileEntry>>(json);
            if (items is null)
            {
                return;
            }

            _entries.Clear();
            _entries.AddRange(items.Where(item => File.Exists(item.FilePath)).Take(MaxRecentFiles));
        }
        catch
        {
            _entries.Clear();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_recentFilePath)!);
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_recentFilePath, json);
    }
}
