using NurSapat.App.Models.Users;
using NurSapat.App.Services.Users;
using NurSapat.Core.Interfaces;

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
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserSessionService sessionService,
        IUsersStore usersStore,
        IUserRepository userRepository)
    {
        _sessionService = sessionService;
        _usersStore = usersStore;
        _userRepository = userRepository;
    }

    public UserAccountItem? CurrentUser => _sessionService.CurrentUser;

    public async Task<UserPasswordChangeResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser is null)
        {
            return new UserPasswordChangeResult(false, "Пользователь не авторизован.");
        }

        var account = _usersStore.GetById(currentUser.Id) ?? currentUser;
        if (!account.VerifyPassword(currentPassword))
        {
            return new UserPasswordChangeResult(false, "Текущий пароль введён неверно.");
        }

        account.SetPassword(newPassword);
        _usersStore.Update(account);

        try
        {
            var dbUser = await _userRepository.GetByIdAsync(account.Id)
                ?? await _userRepository.GetByUsernameAsync(account.Username);
            if (dbUser is not null)
            {
                await _userRepository.UpdatePasswordAsync(dbUser.Id, account.PasswordHash);
            }
        }
        catch
        {
            // Локальный справочник пользователей обновлён; БД может быть недоступна.
        }

        _sessionService.SetCurrentUser(account);
        return new UserPasswordChangeResult(true, "Пароль успешно изменен!");
    }
}
