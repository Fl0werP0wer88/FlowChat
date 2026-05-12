using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
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
    // Most services are Singleton because they either wrap a shared long-lived TCP connection (Redis), hold in-memory state shared across all SignalR connections, or are stateless and safe to reuse — creating them per-request would waste resources without any benefit
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration,
            typeof(InfrastructureServiceRegistration).Assembly,
            typeof(RealtimeRoutingSettingsSection).Assembly);

        // Override connection string from ConnectionStrings section after appsettings binding
        services.PostConfigure<RealtimeConnectionsSettingsSection>(settings =>
        {
            var cs = configuration.GetConnectionString(RealtimeConnectionsSettingsSection.RedisConnectionStringName);
            if (!string.IsNullOrEmpty(cs))
                settings.RedisConnectionString = cs;
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(
                sp.GetRequiredService<IOptions<RealtimeConnectionsSettingsSection>>().Value.RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.TryAddSingleton<IActiveConnectionsTracker, InMemoryActiveConnectionsTracker>();
        services.TryAddSingleton<RealtimeConnectionRedisRepository>();
        services.TryAddSingleton<IRealtimeConnectionRedisRepository>(sp => sp.GetRequiredService<RealtimeConnectionRedisRepository>());
        services.TryAddSingleton<IUserInstanceRoutingReader>(sp => sp.GetRequiredService<RealtimeConnectionRedisRepository>());
        services.TryAddSingleton<IRealtimeConnectionRegistry, RealtimeConnectionRegistry>();
        services.TryAddSingleton<IRealtimeInstanceAddressResolver, ConfiguredRealtimeInstanceAddressResolver>();

        services.AddFlowChatHttpClient<IPresenceInternalApiClient, PresenceInternalApiClient, PresenceServiceSettingsSection>();
        services.AddFlowChatHttpClient<IChatServiceInternalApiClient, ChatServiceInternalApiClient, ChatServiceSettingsSection>();

        services.AddFlowChatHttpClient<IRealtimeInstanceInternalApiClient, RealtimeInstanceInternalApiClient, InternalApiSettingsSection>();
        services.AddScoped<IRealtimeEventRouter, RealtimeEventRouter>();

        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }
}
