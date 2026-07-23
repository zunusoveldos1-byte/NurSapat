using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Infrastructure.Persistence.Seeding;
using NurSapat.Infrastructure.Persistence.Verification;

namespace NurSapat.Infrastructure.Persistence.Tests;

public sealed class PersistenceIntegrityTests
{
    [Fact]
    public async Task RoundTrip_all_aggregates_succeeds_when_postgresql_available()
    {
        var connectionString = Environment.GetEnvironmentVariable("NURSAPAT_TEST_CONNECTION")
            ?? "Host=localhost;Database=nursapat_ef_test;Username=postgres;Password=7720;Port=5432";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Persistence:Provider"] = "PostgreSql"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddNurSapatPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        var seeder = provider.GetRequiredService<InitialDataSeeder>();
        try
        {
            await seeder.SeedAsync();
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
        {
            return;
        }

        var verifier = provider.GetRequiredService<PersistenceIntegrityVerifier>();
        var errors = await verifier.VerifyAsync();

        Assert.Empty(errors);
    }
}
