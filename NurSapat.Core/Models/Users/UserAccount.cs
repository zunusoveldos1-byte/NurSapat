namespace NurSapat.Core.Models.Users;

/// <summary>Persisted application user account (ERP login / permissions).</summary>
public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int GroupNumber { get; set; } = 1;

    /// <summary>JSON-serialized permission flags matching App UserPermissions.</summary>
    public string PermissionsJson { get; set; } = "{}";
}
