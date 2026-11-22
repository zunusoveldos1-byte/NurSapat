using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Models.Users;
using NurSapat.Infrastructure.Persistence.Repositories;
using NurSapat.Infrastructure.Persistence.Auth;

namespace NurSapat.Infrastructure.Persistence.Tests;

public sealed class LocalAuthenticationTests
{
    [Theory]
    [InlineData("admin", "1234", true)]
    [InlineData(" ADMIN ", "1234", true)]
    [InlineData("admin", "wrong", false)]
    [InlineData("unknown", "1234", false)]
    public async Task LoginChecksPersistedPassword(string username, string password, bool expected)
    {
        var options = new DbContextOptionsBuilder<NurSapatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var repository = new UserAccountRepository(new Factory(options));
        await repository.SaveAsync(new UserAccount
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
            GroupNumber = 2,
            PermissionsJson = "{\"canManageUsers\":false}"
        });
        var result = await new LocalAuthenticationService(repository).SignInAsync(username, password);
        Assert.Equal(expected, result is not null);
        if (result is not null)
        {
            Assert.Equal(2, result.GroupNumber);
            Assert.Equal("{\"canManageUsers\":false}", result.PermissionsJson);
        }
    }

    private sealed class Factory(DbContextOptions<NurSapatDbContext> options) : IDbContextFactory<NurSapatDbContext>
    {
        public NurSapatDbContext CreateDbContext() => new(options);
    }
}
