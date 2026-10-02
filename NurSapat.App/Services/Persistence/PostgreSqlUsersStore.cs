using System.Text.Json;
using NurSapat.App.Models.Users;
using NurSapat.App.Services.Users;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Users;

namespace NurSapat.App.Services.Persistence;

public sealed class PostgreSqlUsersStore : IUsersStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IUserAccountRepository _repository;

    public PostgreSqlUsersStore(IUserAccountRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<UserAccountItem> GetAll() =>
        PersistenceSync.Run(() => _repository.GetAllAsync())
            .Select(ToItem)
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public UserAccountItem? GetById(Guid id)
    {
        var user = PersistenceSync.Run(() => _repository.GetByIdAsync(id));
        return user is null ? null : ToItem(user);
    }

    public UserAccountItem? GetByUsername(string username)
    {
        var user = PersistenceSync.Run(() => _repository.GetByUsernameAsync(username));
        return user is null ? null : ToItem(user);
    }

    public void Add(UserAccountItem user) =>
        PersistenceSync.Run(() => _repository.SaveAsync(ToDomain(user)));

    public void Update(UserAccountItem user) =>
        PersistenceSync.Run(() => _repository.UpdateAsync(ToDomain(user)));

    public void Delete(Guid id) =>
        PersistenceSync.Run(() => _repository.DeleteAsync(id));

    private static UserAccountItem ToItem(UserAccount user)
    {
        var permissions = DeserializePermissions(user.PermissionsJson);
        return new UserAccountItem
        {
            Id = user.Id,
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            GroupNumber = user.GroupNumber,
            Permissions = permissions
        };
    }

    private static UserAccount ToDomain(UserAccountItem item) => new()
    {
        Id = item.Id,
        Username = item.Username,
        PasswordHash = item.PasswordHash,
        GroupNumber = item.GroupNumber,
        PermissionsJson = JsonSerializer.Serialize(item.Permissions, JsonOptions)
    };

    private static UserPermissions DeserializePermissions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UserPermissions();
        }

        try
        {
            return JsonSerializer.Deserialize<UserPermissions>(json, JsonOptions) ?? new UserPermissions();
        }
        catch
        {
            return new UserPermissions();
        }
    }
}
