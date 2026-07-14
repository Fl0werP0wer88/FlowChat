using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.InternalApis.ChatService;
using FlowChat.RealtimeService.Infrastructure.InternalApis.PresenceService;
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

public static class ApiInfrastructureServiceRegistration
{
    public static IServiceCollection AddApiInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonInfrastructureServices(configuration);
        services.TryAddSingleton<IActiveConnectionsTracker, InMemoryActiveConnectionsTracker>();
        services.TryAddSingleton<IRealtimeConnectionRegistry, RealtimeConnectionRegistry>();
        services.AddFlowChatHttpClient<IPresenceInternalApiClient, PresenceInternalApiClient, PresenceServiceSettingsSection>();
        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }
}

public static class ConsumerInfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonInfrastructureServices(configuration);
        services.AddScoped<IRealtimeEventRouter, WorkerRealtimeEventRouter>();

        return services;
    }
}

internal static class CommonInfrastructureServiceRegistration
{
    public static IServiceCollection AddCommonInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration,
            typeof(CommonInfrastructureServiceRegistration).Assembly,
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
