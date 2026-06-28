using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.ChatService;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Presence;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCoreInfrastructureServices(configuration);
        services.TryAddSingleton<IActiveConnectionsTracker, InMemoryActiveConnectionsTracker>();
        services.TryAddSingleton<IRealtimeConnectionRegistry, RealtimeConnectionRegistry>();
        services.AddFlowChatHttpClient<IPresenceInternalApiClient, PresenceInternalApiClient, PresenceServiceSettingsSection>();
        services.AddScoped<IRealtimeEventRouter, RealtimeEventRouter>();
        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }

    public static IServiceCollection AddWorkerInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCoreInfrastructureServices(configuration);
        services.AddScoped<IRealtimeEventRouter, WorkerRealtimeEventRouter>();

        return services;
    }

    private static IServiceCollection AddCoreInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration,
            typeof(InfrastructureServiceRegistration).Assembly,
            typeof(RealtimeRoutingSettingsSection).Assembly);

        services.PostConfigure<RealtimeConnectionsSettingsSection>(settings =>
        {
            var connectionString = configuration.GetConnectionString(RealtimeConnectionsSettingsSection.RedisConnectionStringName);
            if (!string.IsNullOrEmpty(connectionString))
            {
                settings.RedisConnectionString = connectionString;
            }
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(
                sp.GetRequiredService<IOptions<RealtimeConnectionsSettingsSection>>().Value.RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.TryAddSingleton<RealtimeConnectionRedisRepository>();
        services.TryAddSingleton<IRealtimeConnectionRedisRepository>(sp => sp.GetRequiredService<RealtimeConnectionRedisRepository>());
        services.TryAddSingleton<IUserInstanceRoutingReader>(sp => sp.GetRequiredService<RealtimeConnectionRedisRepository>());
        services.TryAddSingleton<IRealtimeInstanceAddressResolver, ConfiguredRealtimeInstanceAddressResolver>();

        services.AddFlowChatHttpClient<IChatServiceInternalApiClient, ChatServiceInternalApiClient, ChatServiceSettingsSection>();
        services.AddFlowChatHttpClient<IRealtimeInstanceInternalApiClient, RealtimeInstanceInternalApiClient, InternalApiSettingsSection>();

        return services;
    }
}
