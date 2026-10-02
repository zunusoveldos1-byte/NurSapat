using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Infrastructure.Persistence.Abstractions;
using NurSapat.Infrastructure.Persistence.Repositories;
using NurSapat.Infrastructure.Persistence.Seeding;
using NurSapat.Infrastructure.Persistence.Verification;

namespace NurSapat.Infrastructure.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNurSapatPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        var options = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()
            ?? new PersistenceOptions();

        if (options.Provider != PersistenceProvider.PostgreSql)
        {
            return services;
        }

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{options.ConnectionStringName}' is required when Persistence:Provider is PostgreSql.");
        }

        services.AddDbContextFactory<NurSapatDbContext>(dbOptions =>
            dbOptions.UseNpgsql(connectionString));

        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<ISupplierRepository, SupplierRepository>();
        services.AddSingleton<IErpMaterialRepository, ErpMaterialRepository>();
        services.AddSingleton<IWarehouseRepository, WarehouseRepository>();
        services.AddSingleton<ICurrencyRateRepository, CurrencyRateRepository>();
        services.AddSingleton<InitialDataSeeder>();
        services.AddSingleton<PersistenceIntegrityVerifier>();

        return services;
    }

    public static bool IsPostgreSqlPersistence(this IConfiguration configuration)
    {
        var options = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()
            ?? new PersistenceOptions();

        return options.Provider == PersistenceProvider.PostgreSql;
    }
}
