using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NurSapat.Infrastructure.Sync.Options;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace NurSapat.Infrastructure.Sync.Auth;

/// <summary>Persists Supabase Gotrue session to %APPDATA%/NurSapat/supabase-session.json.</summary>
public sealed class FileSessionPersistence : IGotrueSessionPersistence<Session>
{
    private readonly string _filePath;
    private readonly ILogger<FileSessionPersistence>? _logger;
    private readonly object _lock = new();

    public FileSessionPersistence(IOptions<SyncOptions> options, ILogger<FileSessionPersistence>? logger = null)
    {
        _ = options;
        _logger = logger;
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NurSapat");
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "supabase-session.json");
    }

    public void SaveSession(Session session)
    {
        try
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(session);
                File.WriteAllText(_filePath, json);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save Supabase session to {Path}", _filePath);
        }
    }

    public void DestroySession()
    {
        try
        {
            lock (_lock)
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to destroy Supabase session file {Path}", _filePath);
        }
    }

    public Session? LoadSession()
    {
        try
        {
            lock (_lock)
            {
                if (!File.Exists(_filePath))
                {
                    return null;
                }

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<Session>(json);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load Supabase session from {Path}", _filePath);
            return null;
        }
    }
}
