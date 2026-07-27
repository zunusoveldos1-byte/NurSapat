using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Internal;
using NurSapat.Infrastructure.Updates.Options;
using NurSapat.Infrastructure.Updates.Services;

namespace NurSapat.Infrastructure.Updates.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNurSapatUpdates(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UpdateOptions>(configuration.GetSection(UpdateOptions.SectionName));
        services.AddSingleton<UpdateHttpClientFactory>();
        services.AddSingleton<IUpdateLogger, FileUpdateLogger>();
        services.AddSingleton<IVersionService, VersionService>();
        services.AddSingleton<IUpdateSettingsService, UpdateSettingsService>();
        services.AddSingleton<IUpdateReleaseProvider, GitHubReleaseProvider>();
        services.AddSingleton<IUpdateDownloader, UpdateDownloader>();
        services.AddSingleton<IUpdateValidator, UpdateValidator>();
        services.AddSingleton<UpdateFileReplacer>();
        services.AddSingleton<IRollbackService, RollbackService>();
        services.AddSingleton<IUpdateInstaller, UpdateInstaller>();
        services.AddSingleton<IUpdaterLauncher, UpdaterLauncher>();
        services.AddSingleton<IUpdateService, UpdateService>();
        return services;
    }
}
