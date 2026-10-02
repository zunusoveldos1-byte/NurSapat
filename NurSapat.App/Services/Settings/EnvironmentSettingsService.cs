using System.Text.Json;
using System.Text.RegularExpressions;
using NurSapat.App.Models.Settings;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;
using CoreEnvironmentSettings = NurSapat.Core.Settings.EnvironmentSettings;

namespace NurSapat.App.Services.Settings;

public interface IEnvironmentSettingsService
{
    Models.Settings.EnvironmentSettings Current { get; }

    ConfigurationProfile? ActiveProfile { get; }

    event EventHandler? ProfileChanged;

    void Load();

    void Save(Models.Settings.EnvironmentSettings settings);

    IReadOnlyList<ConfigurationProfile> LoadAvailableProfiles();

    void SwitchToProfile(ConfigurationProfile profile);

    ConfigurationProfile CreateProfile(string profileName, bool copyFromCurrent = true);

    void DeleteProfile(ConfigurationProfile profile);

    bool CanDeleteProfile(ConfigurationProfile profile);

    void ApplyToNewOrderHeader(OrderHeader header);

    string HeaderImageFilePath { get; }

    void LoadHeaderImage(string sourceFilePath);
}

public sealed class EnvironmentSettingsService : IEnvironmentSettingsService
{
    private readonly string _appDataPath;
    private readonly string _profilesIndexPath;
    private readonly string _legacySettingsFilePath;
    private readonly string _headerImageFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private ConfigurationProfilesIndex _profilesIndex = new();

    public EnvironmentSettingsService(CoreEnvironmentSettings paths)
    {
        _appDataPath = paths.AppDataPath;
        _profilesIndexPath = Path.Combine(_appDataPath, "configuration-profiles.json");
        _legacySettingsFilePath = Path.Combine(_appDataPath, "environment-settings.json");
        _headerImageFilePath = Path.Combine(_appDataPath, "SHAP.BMP");
        Directory.CreateDirectory(_appDataPath);
        Current = CreateDefaults();
        EnsureProfilesInitialized();
        LoadActiveProfileSettings();
        SyncHeaderLoadedState();
    }

    public Models.Settings.EnvironmentSettings Current { get; private set; }

    public ConfigurationProfile? ActiveProfile =>
        _profilesIndex.Profiles.FirstOrDefault(profile => profile.Id == _profilesIndex.ActiveProfileId);

    public event EventHandler? ProfileChanged;

    public string HeaderImageFilePath => _headerImageFilePath;

    public void Load() => LoadActiveProfileSettings();

    public void Save(Models.Settings.EnvironmentSettings settings)
    {
        Current = settings.Clone();
        var activeProfile = ActiveProfile;
        if (activeProfile is null)
        {
            return;
        }

        var profilePath = GetProfileSettingsPath(activeProfile);
        var json = JsonSerializer.Serialize(Current, _jsonOptions);
        File.WriteAllText(profilePath, json);
    }

    public IReadOnlyList<ConfigurationProfile> LoadAvailableProfiles()
    {
        EnsureProfilesInitialized();
        return _profilesIndex.Profiles
            .Select(profile => profile.Clone())
            .OrderByDescending(profile => profile.Id == _profilesIndex.ActiveProfileId)
            .ThenBy(profile => profile.IsAdminProfile() ? 0 : 1)
            .ThenBy(profile => profile.ProfileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void SwitchToProfile(ConfigurationProfile profile)
    {
        EnsureProfilesInitialized();

        var target = _profilesIndex.Profiles.FirstOrDefault(item => item.Id == profile.Id);
        if (target is null)
        {
            throw new InvalidOperationException("Выбранная конфигурация не найдена.");
        }

        if (target.Id == _profilesIndex.ActiveProfileId)
        {
            target.LastActive = DateTime.Now;
            SaveProfilesIndex();
            return;
        }

        Save(Current.Clone());

        _profilesIndex.ActiveProfileId = target.Id;
        target.LastActive = DateTime.Now;
        SaveProfilesIndex();

        LoadActiveProfileSettings();
        SyncHeaderLoadedState();
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public ConfigurationProfile CreateProfile(string profileName, bool copyFromCurrent = true)
    {
        var trimmedName = profileName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ArgumentException("Имя конфигурации не может быть пустым.", nameof(profileName));
        }

        EnsureProfilesInitialized();

        if (_profilesIndex.Profiles.Any(profile =>
                string.Equals(profile.ProfileName, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Конфигурация с таким именем уже существует.");
        }

        Save(Current.Clone());

        var profile = new ConfigurationProfile
        {
            ProfileName = trimmedName,
            ConfigFileName = BuildUniqueConfigFileName(trimmedName),
            LastActive = default
        };

        var settings = copyFromCurrent ? Current.Clone() : CreateDefaults();
        var profilePath = GetProfileSettingsPath(profile);
        File.WriteAllText(profilePath, JsonSerializer.Serialize(settings, _jsonOptions));

        _profilesIndex.Profiles.Add(profile);
        SaveProfilesIndex();

        return profile.Clone();
    }

    public void DeleteProfile(ConfigurationProfile profile)
    {
        if (!CanDeleteProfile(profile))
        {
            throw new InvalidOperationException("Эту конфигурацию удалить нельзя.");
        }

        EnsureProfilesInitialized();

        var existing = _profilesIndex.Profiles.FirstOrDefault(item => item.Id == profile.Id);
        if (existing is null)
        {
            return;
        }

        _profilesIndex.Profiles.Remove(existing);
        SaveProfilesIndex();

        var profilePath = GetProfileSettingsPath(existing);
        if (File.Exists(profilePath))
        {
            File.Delete(profilePath);
        }
    }

    public bool CanDeleteProfile(ConfigurationProfile profile) =>
        !profile.IsAdminProfile() && profile.Id != _profilesIndex.ActiveProfileId;

    public void ApplyToNewOrderHeader(OrderHeader header)
    {
        var today = DateTime.Today;
        header.OrderDate = today;
        header.OrderNumber = OrderNumberGenerator.GenerateNext();
        header.Status = OrderStatus.Draft;

        if (Current.DaysToProduction > 0)
        {
            header.ProductionDate = today.AddDays(Current.DaysToProduction);
        }

        if (Current.DaysToInstallation > 0)
        {
            header.InstallationDate = today.AddDays(Current.DaysToInstallation);
        }

        Current.NextOrderNumber++;
        Save(Current.Clone());
    }

    public void LoadHeaderImage(string sourceFilePath)
    {
        File.Copy(sourceFilePath, _headerImageFilePath, overwrite: true);
        Current.IsHeaderLoaded = true;
        Save(Current.Clone());
    }

    private void EnsureProfilesInitialized()
    {
        if (File.Exists(_profilesIndexPath))
        {
            try
            {
                var json = File.ReadAllText(_profilesIndexPath);
                var loaded = JsonSerializer.Deserialize<ConfigurationProfilesIndex>(json, _jsonOptions);
                if (loaded is { Profiles.Count: > 0 })
                {
                    _profilesIndex = loaded;
                    EnsureAdminProfileExists();
                    return;
                }
            }
            catch
            {
                // Rebuild index below.
            }
        }

        _profilesIndex = BuildInitialProfilesIndex();
        SaveProfilesIndex();
        EnsureAdminProfileExists();
    }

    private ConfigurationProfilesIndex BuildInitialProfilesIndex()
    {
        var admin = ConfigurationProfile.CreateAdmin(DateTime.Now);
        var adminSettings = CreateDefaults();

        if (File.Exists(_legacySettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_legacySettingsFilePath);
                adminSettings = JsonSerializer.Deserialize<Models.Settings.EnvironmentSettings>(json, _jsonOptions)?.Clone()
                    ?? CreateDefaults();
            }
            catch
            {
                adminSettings = CreateDefaults();
            }
        }

        File.WriteAllText(
            GetProfileSettingsPath(admin),
            JsonSerializer.Serialize(adminSettings, _jsonOptions));

        return new ConfigurationProfilesIndex
        {
            ActiveProfileId = admin.Id,
            Profiles = [admin]
        };
    }

    private void EnsureAdminProfileExists()
    {
        var admin = _profilesIndex.Profiles.FirstOrDefault(profile => profile.IsAdminProfile());
        if (admin is null)
        {
            admin = ConfigurationProfile.CreateAdmin(DateTime.Now);
            _profilesIndex.Profiles.Insert(0, admin);

            if (!File.Exists(GetProfileSettingsPath(admin)))
            {
                File.WriteAllText(
                    GetProfileSettingsPath(admin),
                    JsonSerializer.Serialize(CreateDefaults(), _jsonOptions));
            }
        }

        if (_profilesIndex.Profiles.All(profile => profile.Id != _profilesIndex.ActiveProfileId))
        {
            _profilesIndex.ActiveProfileId = admin.Id;
        }

        SaveProfilesIndex();
    }

    private void LoadActiveProfileSettings()
    {
        EnsureProfilesInitialized();

        var activeProfile = ActiveProfile ?? _profilesIndex.Profiles.First();
        _profilesIndex.ActiveProfileId = activeProfile.Id;

        var profilePath = GetProfileSettingsPath(activeProfile);
        if (!File.Exists(profilePath))
        {
            Current = CreateDefaults();
            File.WriteAllText(profilePath, JsonSerializer.Serialize(Current, _jsonOptions));
            return;
        }

        try
        {
            var json = File.ReadAllText(profilePath);
            var loaded = JsonSerializer.Deserialize<Models.Settings.EnvironmentSettings>(json, _jsonOptions);
            Current = loaded?.Clone() ?? CreateDefaults();
        }
        catch
        {
            Current = CreateDefaults();
        }
    }

    private void SaveProfilesIndex()
    {
        var json = JsonSerializer.Serialize(_profilesIndex, _jsonOptions);
        File.WriteAllText(_profilesIndexPath, json);
    }

    private string GetProfileSettingsPath(ConfigurationProfile profile) =>
        Path.Combine(_appDataPath, profile.ConfigFileName);

    private string BuildUniqueConfigFileName(string profileName)
    {
        var slug = Regex.Replace(profileName.Trim().ToLowerInvariant(), @"[^a-z0-9а-яё]+", "_");
        slug = slug.Trim('_');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Guid.NewGuid().ToString("N")[..8];
        }

        var baseName = $"env_settings_{slug}.json";
        if (_profilesIndex.Profiles.All(profile =>
                !string.Equals(profile.ConfigFileName, baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        return $"env_settings_{slug}_{Guid.NewGuid():N}.json";
    }

    private void SyncHeaderLoadedState()
    {
        var isLoaded = File.Exists(_headerImageFilePath);
        if (Current.IsHeaderLoaded == isLoaded)
        {
            return;
        }

        Current.IsHeaderLoaded = isLoaded;
        Save(Current.Clone());
    }

    private static Models.Settings.EnvironmentSettings CreateDefaults() => new();
}
