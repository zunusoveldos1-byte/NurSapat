namespace NurSapat.Infrastructure.Sync.Models;

public sealed class CachedUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset LastLoginAt { get; set; }

    public DateTimeOffset LastOnlineUtc { get; set; }

    public DateTimeOffset LastLocalAppRunUtc { get; set; }
}
