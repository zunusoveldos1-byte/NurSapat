using System.Text.Json;
using CoreEnvironmentSettings = NurSapat.Core.Settings.EnvironmentSettings;

namespace NurSapat.App.Services.Utilities;

public interface IDatabasePasswordService
{
    bool HasMasterPassword { get; }

    void SetMasterPassword(string password);

    bool VerifyMasterPassword(string password);
}

public sealed class DatabasePasswordService : IDatabasePasswordService
{
    private readonly string _settingsPath;
    private DatabaseSecuritySettings _settings;

    public DatabasePasswordService(CoreEnvironmentSettings paths)
    {
        _settingsPath = Path.Combine(paths.AppDataPath, "database-security.json");
        _settings = Load();
    }

    public bool HasMasterPassword => !string.IsNullOrEmpty(_settings.PasswordHash);

    public void SetMasterPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _settings.PasswordHash = string.Empty;
        }
        else
        {
            _settings.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }

        Save();
    }

    public bool VerifyMasterPassword(string password)
    {
        if (string.IsNullOrEmpty(_settings.PasswordHash))
        {
            return string.IsNullOrWhiteSpace(password);
        }

        return BCrypt.Net.BCrypt.Verify(password, _settings.PasswordHash);
    }

    private DatabaseSecuritySettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new DatabaseSecuritySettings();
        }

        try
        {
            return JsonSerializer.Deserialize<DatabaseSecuritySettings>(File.ReadAllText(_settingsPath))
                ?? new DatabaseSecuritySettings();
        }
        catch
        {
            return new DatabaseSecuritySettings();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private sealed class DatabaseSecuritySettings
    {
        public string PasswordHash { get; set; } = string.Empty;
    }
}
