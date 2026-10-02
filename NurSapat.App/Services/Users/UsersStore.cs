using NurSapat.App.Models.Users;

namespace NurSapat.App.Services.Users;

public interface IUsersStore
{
    IReadOnlyList<UserAccountItem> GetAll();

    UserAccountItem? GetById(Guid id);

    UserAccountItem? GetByUsername(string username);

    void Add(UserAccountItem user);

    void Update(UserAccountItem user);

    void Delete(Guid id);
}

public sealed class UsersStore : IUsersStore
{
    private readonly List<UserAccountItem> _users = CreateDemoUsers();

    public IReadOnlyList<UserAccountItem> GetAll() =>
        _users.OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase).ToList();

    public UserAccountItem? GetById(Guid id) => _users.FirstOrDefault(user => user.Id == id);

    public UserAccountItem? GetByUsername(string username) =>
        _users.FirstOrDefault(user =>
            user.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public void Add(UserAccountItem user) => _users.Add(user);

    public void Update(UserAccountItem user)
    {
        var index = _users.FindIndex(item => item.Id == user.Id);
        if (index >= 0)
        {
            _users[index] = user;
        }
    }

    public void Delete(Guid id) => _users.RemoveAll(user => user.Id == id);

    private static List<UserAccountItem> CreateDemoUsers() =>
    [
        UserAccountItem.Create("admin", "admin", 1, UserPermissions.CreateAdmin()),
        UserAccountItem.Create("manager", "manager", 1, new UserPermissions
        {
            CanAccessMaterialsDictionary = true,
            CanAccessEconomics = true,
            CanAccessWarehouse = true,
            CanEditWarehouse = true,
            CanPrintMaterialPrices = true
        }),
        UserAccountItem.Create("operator", "operator", 2, UserPermissions.CreateOperator())
    ];
}
