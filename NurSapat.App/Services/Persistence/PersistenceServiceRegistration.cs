using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Config;
using NurSapat.App.Services.Currency;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Materials;
using NurSapat.App.Services.Orders;
using NurSapat.App.Services.Stock;
using NurSapat.App.Services.Users;
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
        services.AddSingleton<IOrdersStore, PostgreSqlOrdersStore>();
        services.AddSingleton<IUsersStore, PostgreSqlUsersStore>();
        services.AddSingleton<IOrderTemplatesStore, PostgreSqlOrderTemplatesStore>();
        services.AddSingleton<IConstructionElementsStore, PostgreSqlConstructionElementsStore>();
        services.AddSingleton<IExchangeRatesStore, PostgreSqlExchangeRatesStore>();

        return services;
    }
}
