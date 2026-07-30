using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Local;
using NurSapat.Infrastructure.Sync.Options;
using NurSapat.Infrastructure.Sync.Services;

namespace NurSapat.Infrastructure.Sync.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNurSapatSync(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SyncOptions>(configuration.GetSection(SyncOptions.SectionName));

        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<ProtectedSessionCache>();
        services.AddSingleton<DatabaseSaltStore>();
        services.AddSingleton<SupabaseClientFactory>();

        services.AddSingleton<INetworkStatusService, NetworkStatusService>();
        services.AddSingleton<IConnectivityMonitor>(
            provider => provider.GetRequiredService<INetworkStatusService>());
        services.AddSingleton<ILocalDbService, LocalDbService>();
        services.AddSingleton<ISupabaseAuthService, AuthService>();
        services.AddSingleton<ICachedUserRepository, CachedUserRepository>();
        services.AddSingleton<IGitHubReleaseCache, GitHubReleaseCache>();
        services.AddSingleton<IOfflineMaterialRepository, OfflineMaterialRepository>();
        services.AddSingleton<IOfflineOrderRepository, OfflineOrderRepository>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IBackgroundSynchronizer, BackgroundSynchronizer>();

        return services;
    }
}
