using System.Text.Json;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class UpdateSettingsService : IUpdateSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _settingsFilePath;
    private readonly IUpdateLogger _logger;
    private readonly object _sync = new();

    public UpdateSettingsService(IUpdateLogger logger)
    {
        _logger = logger;
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NurSapat");
        Directory.CreateDirectory(appData);
        _settingsFilePath = Path.Combine(appData, "update-settings.json");
        Current = new UpdateSettings();
    }

    public UpdateSettings Current { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var loaded = await JsonSerializer.DeserializeAsync<UpdateSettings>(stream, JsonOptions, cancellationToken);
            if (loaded is not null)
            {
                lock (_sync)
                {
                    Current = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Не удалось загрузить настройки обновлений.", ex);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            UpdateSettings snapshot;
            lock (_sync)
            {
                snapshot = Clone(Current);
            }

            await using var stream = File.Create(_settingsFilePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Не удалось сохранить настройки обновлений.", ex);
        }
    }

    public void SetSkippedVersion(Version version)
    {
        lock (_sync)
        {
            Current.SkippedVersion = version.ToString(3);
        }
    }

    public void ClearSkippedVersion()
    {
        lock (_sync)
        {
            Current.SkippedVersion = null;
        }
    }

    public void RecordCheck(DateTimeOffset utc, Version? latestVersion)
    {
        lock (_sync)
        {
            Current.LastCheckUtc = utc;
            Current.LastKnownLatestVersion = latestVersion?.ToString(3);
        }
    }

    public void RecordSuccessfulUpdate(Version version)
    {
        lock (_sync)
        {
            Current.LastUpdateUtc = DateTimeOffset.UtcNow;
            Current.LastKnownLatestVersion = version.ToString(3);
            Current.SkippedVersion = null;
        }
    }

    public void SetPendingSuccessVersion(Version version)
    {
        lock (_sync)
        {
            Current.PendingSuccessVersion = version.ToString(3);
        }
    }

    public void ClearPendingSuccessVersion()
    {
        lock (_sync)
        {
            Current.PendingSuccessVersion = null;
        }
    }

    private static UpdateSettings Clone(UpdateSettings source) => new()
    {
        AutoCheckEnabled = source.AutoCheckEnabled,
        Channel = source.Channel,
        SkippedVersion = source.SkippedVersion,
        LastKnownLatestVersion = source.LastKnownLatestVersion,
        LastCheckUtc = source.LastCheckUtc,
        LastUpdateUtc = source.LastUpdateUtc,
        PendingSuccessVersion = source.PendingSuccessVersion
    };
}
