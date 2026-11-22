using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Infrastructure.Persistence.Auth;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Infrastructure.Persistence.Seeding;

var root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(root, "NurSapat.App", "appsettings.json"))
    .AddEnvironmentVariables()
    .Build();
var services = new ServiceCollection().AddNurSapatPersistence(configuration);
await using var provider = services.BuildServiceProvider();
await provider.GetRequiredService<InitialDataSeeder>().SeedAsync();
if (args.Contains("--prepare-only", StringComparer.Ordinal))
{
    Console.WriteLine("Local PostgreSQL schema is ready.");
    return;
}
var users = provider.GetRequiredService<IUserAccountRepository>();
var admin = await users.GetByUsernameAsync("admin")
    ?? throw new InvalidOperationException("Admin account was not seeded.");
// Explicit local setup sets the requested application password.
admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234");
await users.UpdateAsync(admin);
var auth = provider.GetRequiredService<LocalAuthenticationService>();
if (await auth.SignInAsync("admin", "1234") is null
    || await auth.SignInAsync("admin", "wrong") is not null)
    throw new InvalidOperationException("Local login verification failed.");
Console.WriteLine("Local PostgreSQL schema ready; admin/1234 login verified.");
