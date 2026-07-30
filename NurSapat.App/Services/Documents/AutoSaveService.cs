using NurSapat.Core.Models.Project;
using NurSapat.Core.Settings;

namespace NurSapat.App.Services.Documents;

public interface IAutoSaveService : IDisposable
{
    void Start(IDocumentManager documentManager);

    void Stop();

    string? AutoSaveFilePath { get; }

    bool HasAutoSaveFile { get; }

    Task SaveAutoSaveAsync(CancellationToken cancellationToken = default);

    void ClearAutoSave();
}

public sealed class AutoSaveService : IAutoSaveService
{
    private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromMinutes(5);

    private readonly IProjectSerializer _serializer;
    private readonly string _autoSavePath;
    private Timer? _timer;
    private IDocumentManager? _documentManager;

    public AutoSaveService(IProjectSerializer serializer, EnvironmentSettings environmentSettings)
    {
        _serializer = serializer;
        _autoSavePath = Path.Combine(environmentSettings.AppDataPath, "autosave", "autosave.zp");
    }

    public string? AutoSaveFilePath => File.Exists(_autoSavePath) ? _autoSavePath : null;

    public bool HasAutoSaveFile => File.Exists(_autoSavePath);

    public void Start(IDocumentManager documentManager)
    {
        _documentManager = documentManager;
        _timer?.Dispose();
        _timer = new Timer(OnTimerTick, null, AutoSaveInterval, AutoSaveInterval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _documentManager = null;
    }

    public async Task SaveAutoSaveAsync(CancellationToken cancellationToken = default)
    {
        if (_documentManager is null || !_documentManager.HasOpenDocument)
        {
            return;
        }

        var snapshot = _documentManager.CreateSnapshot();
        await _serializer.SaveAsync(snapshot, _autoSavePath, cancellationToken);
    }

    public void ClearAutoSave()
    {
        if (File.Exists(_autoSavePath))
        {
            File.Delete(_autoSavePath);
        }
    }

    private async void OnTimerTick(object? state)
    {
        if (_documentManager is null || !_documentManager.IsModified)
        {
            return;
        }

        try
        {
            await SaveAutoSaveAsync();
        }
        catch
        {
            // Автосохранение не должно прерывать работу пользователя.
        }
    }

    public void Dispose() => Stop();
}
