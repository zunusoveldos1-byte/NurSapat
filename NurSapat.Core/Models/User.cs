using System;

namespace NurSapat.Core.Models;

public enum UserRole { Admin, User }

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }

    public static User Create(string username, string password)
    {
        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var hash = BCrypt.Net.BCrypt.HashPassword(password, salt);
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hash,
            Salt = salt,
            Role = UserRole.User
        };
    }
}