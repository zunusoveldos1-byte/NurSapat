using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NurSapat.Infrastructure.Persistence;

public sealed class NurSapatDbContextFactory : IDesignTimeDbContextFactory<NurSapatDbContext>
{
    public NurSapatDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var appSettingsPath = Path.Combine(basePath, "NurSapat.App", "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(basePath, "..", "NurSapat.App", "appsettings.json");
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionStringName = configuration["Persistence:ConnectionStringName"] ?? "PostgreSQL";
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? "Host=localhost;Database=nursapat_db;Username=postgres;Password=postgres;Port=5432";

        var optionsBuilder = new DbContextOptionsBuilder<NurSapatDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NurSapatDbContext(optionsBuilder.Options);
    }
}
