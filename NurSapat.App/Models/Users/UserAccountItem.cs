namespace NurSapat.App.Models.Users;

public sealed class UserAccountItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int GroupNumber { get; set; } = 1;

    public UserPermissions Permissions { get; set; } = new();

    public bool VerifyPassword(string password) =>
        !string.IsNullOrEmpty(PasswordHash)
        && BCrypt.Net.BCrypt.Verify(password, PasswordHash);

    public void SetPassword(string password) =>
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

    public static UserAccountItem Create(string username, string password, int groupNumber, UserPermissions permissions)
    {
        var item = new UserAccountItem
        {
            Username = username,
            GroupNumber = groupNumber,
            Permissions = permissions.Clone()
        };
        item.SetPassword(password);
        return item;
    }

    public UserAccountItem Clone() => new()
    {
        Id = Id,
        Username = Username,
        PasswordHash = PasswordHash,
        GroupNumber = GroupNumber,
        Permissions = Permissions.Clone()
    };
}
