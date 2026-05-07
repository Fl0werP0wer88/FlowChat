using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
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

        services.AddFlowChatHttpClient<IPresenceInternalApiClient, PresenceInternalApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<PresenceServiceSettingsSection>>().Value;
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

        services.AddFlowChatHttpClient<IRealtimeInstanceInternalApiClient, RealtimeInstanceInternalApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<InternalApiSettingsSection>>().Value;
            client.DefaultRequestHeaders.Remove(RealtimeInstanceInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                client.DefaultRequestHeaders.Add(RealtimeInstanceInternalApiClient.ApiKeyHeaderName, settings.ApiKey);
            }
        });
        services.AddScoped<IRealtimeEventRouter, RealtimeEventRouter>();

        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }
}
