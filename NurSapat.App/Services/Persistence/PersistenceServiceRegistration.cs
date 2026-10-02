using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Currency;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Materials;
using NurSapat.App.Services.Stock;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Infrastructure.Persistence.DependencyInjection;

namespace NurSapat.App.Services.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddAppPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddNurSapatPersistence(configuration);

        if (!configuration.IsPostgreSqlPersistence())
        {
            services.AddSingleton<ICurrencyRateRepository, InMemoryCurrencyRateRepository>();
            return services;
        }

        services.AddSingleton<ICustomersStore, PostgreSqlCustomersStore>();
        services.AddSingleton<ISuppliersStore, PostgreSqlSuppliersStore>();
        services.AddSingleton<IMaterialsDetailStore, PostgreSqlMaterialsDetailStore>();
        services.AddSingleton<IStockMaterialsStore, PostgreSqlStockMaterialsStore>();
        services.AddSingleton<IStockFinishedGoodsStore, PostgreSqlStockFinishedGoodsStore>();

        return services;
    }
}
