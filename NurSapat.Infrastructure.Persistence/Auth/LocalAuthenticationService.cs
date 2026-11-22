using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Users;

namespace NurSapat.Infrastructure.Persistence.Auth;

public sealed class LocalAuthenticationService(IUserAccountRepository users)
{
    public async Task<UserAccount?> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        var user = await users.GetByUsernameAsync(username.Trim(), cancellationToken);
        return user is not null && !string.IsNullOrEmpty(user.PasswordHash)
            && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }
}
