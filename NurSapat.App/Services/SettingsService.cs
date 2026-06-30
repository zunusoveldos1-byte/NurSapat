using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace NurSapat.App.Services;

public sealed class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, string> _settings = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService()
    {
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
        SaveSettings();
    }

    private void LoadSettings()
    {
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

    private void SaveSettings()
    {
        string json = JsonSerializer.Serialize(_settings, _jsonOptions);
        File.WriteAllText(_settingsFilePath, json);
    }
}