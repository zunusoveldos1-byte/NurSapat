using System.ComponentModel.DataAnnotations.Schema; // Оставляем только это
using System;

namespace NurSapat.Core.Models;

public enum UserRole { Admin, User }

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;

    [Column("passwordhash")]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }

    public static User Create(string username, string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hash,
            Role = UserRole.User
        };
    }
}