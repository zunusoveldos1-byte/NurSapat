using NurSapat.App.Models.Users;

namespace NurSapat.App.Services.Users;

public interface IUserService
{
    UserAccountItem? CurrentUser { get; }

    Task<UserPasswordChangeResult> ChangePasswordAsync(string currentPassword, string newPassword);
}

public sealed record UserPasswordChangeResult(bool Success, string Message);

public sealed class UserService : IUserService
{
    private readonly IUserSessionService _sessionService;
    private readonly IUsersStore _usersStore;

    public UserService(
        IUserSessionService sessionService,
        IUsersStore usersStore)
    {
        _sessionService = sessionService;
        _usersStore = usersStore;
    }

    public UserAccountItem? CurrentUser => _sessionService.CurrentUser;

    public Task<UserPasswordChangeResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser is null)
        {
            return Task.FromResult(new UserPasswordChangeResult(false, "Пользователь не авторизован."));
        }

        var account = _usersStore.GetById(currentUser.Id) ?? currentUser;
        if (!account.VerifyPassword(currentPassword))
        {
            return Task.FromResult(new UserPasswordChangeResult(false, "Текущий пароль введён неверно."));
        }

        account.SetPassword(newPassword);
        _usersStore.Update(account);
        _sessionService.SetCurrentUser(account);

        return Task.FromResult(new UserPasswordChangeResult(true, "Пароль успешно изменен!"));
    }
}
