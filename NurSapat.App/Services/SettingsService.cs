using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using NurSapat.App.Services.Persistence;
using NurSapat.Core.Interfaces.Persistence;

namespace NurSapat.App.Services;

/// <summary>
/// Application UI settings (RememberMe, etc.).
/// When PostgreSQL persistence is active, values are stored in the app_settings table.
/// Without a repository, falls back to %LocalAppData%\NurSapat\settings.json
/// (machine-local only — not business/working data).
/// Environment profiles/paths stay in local files via EnvironmentSettingsService.
/// </summary>
public sealed class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, string> _settings = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAppSettingRepository? _repository;

    public SettingsService(IAppSettingRepository? repository = null)
    {
        _repository = repository;

        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "NurSapat");
        if (!Directory.Exists(appFolderPath))
        {
            Directory.CreateDirectory(appFolderPath);
        }
        _settingsFilePath = Path.Combine(appFolderPath, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        LoadSettings();
    }

    public Guid CurrentUserId { get; set; }

    [return: MaybeNull]
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (_settings.TryGetValue(key, out string? stringValue))
        {
            return JsonSerializer.Deserialize<T>(stringValue, _jsonOptions) ?? defaultValue;
        }
        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        string stringValue = value == null ? "" : JsonSerializer.Serialize(value, _jsonOptions);
        _settings[key] = stringValue;
        SaveSettings(key, stringValue);
    }

    private void LoadSettings()
    {
        if (_repository is not null)
        {
            try
            {
                var loaded = PersistenceSync.Run(() => _repository.GetAllAsync());
                _settings.Clear();
                foreach (var kvp in loaded)
                {
                    _settings[kvp.Key] = kvp.Value;
                }

                return;
            }
            catch
            {
                // Fall back to local JSON if the database is unavailable.
            }
        }

        if (File.Exists(_settingsFilePath))
        {
            string json = File.ReadAllText(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
            if (loaded != null)
            {
                _settings.Clear();
                foreach (var kvp in loaded)
                {
                    _settings[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    private void SaveSettings(string? changedKey = null, string? changedValue = null)
    {
        if (_repository is not null && changedKey is not null)
        {
            try
            {
                PersistenceSync.Run(() => _repository.SetAsync(changedKey, changedValue ?? string.Empty));
                return;
            }
            catch
            {
                // Fall back to local JSON.
            }
        }

        string json = JsonSerializer.Serialize(_settings, _jsonOptions);
        File.WriteAllText(_settingsFilePath, json);
    }
}
