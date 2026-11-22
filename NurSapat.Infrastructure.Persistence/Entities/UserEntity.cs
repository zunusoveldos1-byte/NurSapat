namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int GroupNumber { get; set; } = 1;

    public string PermissionsJson { get; set; } = "{}";
}
