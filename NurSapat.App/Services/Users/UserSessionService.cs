using NurSapat.App.Models.Users;

namespace NurSapat.App.Services.Users;

public interface IUserSessionService
{
    UserAccountItem? CurrentUser { get; }

    bool IsAdministrator { get; }

    bool IsOffline { get; }

    void SetCurrentUser(UserAccountItem user, bool isOffline = false);

    void Clear();
}

public sealed class UserSessionService : IUserSessionService
{
    public UserAccountItem? CurrentUser { get; private set; }

    public bool IsAdministrator => CurrentUser?.Permissions.CanManageUsers == true;

    public bool IsOffline { get; private set; }

    public void SetCurrentUser(UserAccountItem user, bool isOffline = false)
    {
        CurrentUser = user.Clone();
        IsOffline = isOffline;
    }

    public void Clear()
    {
        CurrentUser = null;
        IsOffline = false;
    }
}
