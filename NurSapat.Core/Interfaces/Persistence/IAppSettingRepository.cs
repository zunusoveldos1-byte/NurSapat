namespace NurSapat.Core.Interfaces.Persistence;

/// <summary>Key-value application settings persisted in PostgreSQL (RememberMe, etc.).</summary>
public interface IAppSettingRepository
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
