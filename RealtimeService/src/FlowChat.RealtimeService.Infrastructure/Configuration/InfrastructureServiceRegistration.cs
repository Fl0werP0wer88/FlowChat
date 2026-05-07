using FlowChat.Core.Contracts;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Presence;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<ISettingsProvider>(new AppSettingsProvider(configuration));
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsProvider>().GetSection<RealtimeConnectionsSettingsSection>();
            settings.RedisConnectionString = configuration.GetConnectionString(RealtimeConnectionsSettingsSection.RedisConnectionStringName)
                ?? settings.RedisConnectionString;
            return settings;
        });
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(sp.GetRequiredService<RealtimeConnectionsSettingsSection>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.TryAddSingleton<RedisUnitOfWork>();
        services.TryAddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<RedisUnitOfWork>());
        services.TryAddSingleton<IRedisTransactionContext>(sp => sp.GetRequiredService<RedisUnitOfWork>());
        services.TryAddSingleton<IActiveConnectionsTracker, InMemoryActiveConnectionsTracker>();
        services.TryAddSingleton<RealtimeConnectionRedisRepository>();
        services.TryAddSingleton<IRealtimeConnectionRedisRepository>(sp => sp.GetRequiredService<RealtimeConnectionRedisRepository>());
        services.TryAddSingleton<IUserInstanceRoutingReader>(sp => sp.GetRequiredService<RealtimeConnectionRedisRepository>());
        services.TryAddSingleton<IRealtimeConnectionRegistry, RealtimeConnectionRegistry>();
        services.TryAddSingleton(sp => sp.GetRequiredService<ISettingsProvider>().GetSection<RealtimeInstancesSettingsSection>());
        services.TryAddSingleton<IRealtimeInstanceAddressResolver, ConfiguredRealtimeInstanceAddressResolver>();

        services.TryAddSingleton(sp => sp.GetRequiredService<ISettingsProvider>().GetSection<PresenceServiceSettingsSection>());
        services.TryAddSingleton(sp => sp.GetRequiredService<ISettingsProvider>().GetSection<InternalApiSettingsSection>());
        services.AddHttpClient(PresenceInternalApiClient.HttpClientName, (sp, client) =>
        {
            var settings = sp.GetRequiredService<PresenceServiceSettingsSection>();
            if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException("PresenceService:BaseUrl must be an absolute URI.");
            }

            client.BaseAddress = baseAddress;
            if (!string.IsNullOrWhiteSpace(settings.InternalApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Internal-Api-Key", settings.InternalApiKey);
            }
        });
        services.TryAddSingleton<IPresenceInternalApiClient, PresenceInternalApiClient>();
        services.AddHttpClient(RealtimeInstanceInternalApiClient.HttpClientName, (sp, client) =>
        {
            var settings = sp.GetRequiredService<InternalApiSettingsSection>();
            client.DefaultRequestHeaders.Remove(RealtimeInstanceInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                client.DefaultRequestHeaders.Add(RealtimeInstanceInternalApiClient.ApiKeyHeaderName, settings.ApiKey);
            }
        });
        services.AddScoped<IRealtimeInstanceInternalApiClient>(sp =>
            new RealtimeInstanceInternalApiClient(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(RealtimeInstanceInternalApiClient.HttpClientName)));
        services.AddScoped<IRealtimeEventRouter, RealtimeEventRouter>();

        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }
}
