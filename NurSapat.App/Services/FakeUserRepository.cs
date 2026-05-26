using System;
using System.Threading.Tasks;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;

namespace NurSapat.App.Services;

public class FakeUserRepository : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username)
    {
        if (username == "admin")
        {
            return Task.FromResult<User?>(new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Salt = "",
                Role = UserRole.Admin
            });
        }
        return Task.FromResult<User?>(null);
    }

    public Task CreateAsync(User user) => Task.CompletedTask;
}