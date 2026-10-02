using NurSapat.App.Models.Users;

namespace NurSapat.App.Services.Users;

public interface IUserSessionService
{
    UserAccountItem? CurrentUser { get; }

    bool IsAdministrator { get; }

    void SetCurrentUser(UserAccountItem user);

    void Clear();
}

public sealed class UserSessionService : IUserSessionService
{
    public UserAccountItem? CurrentUser { get; private set; }

    public bool IsAdministrator => CurrentUser?.Permissions.CanManageUsers == true;

    public void SetCurrentUser(UserAccountItem user) => CurrentUser = user.Clone();

    public void Clear() => CurrentUser = null;
}
