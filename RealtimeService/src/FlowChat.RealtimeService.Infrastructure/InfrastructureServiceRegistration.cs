using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.Kafka;
using FlowChat.RealtimeService.Infrastructure.Presence;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.ConnectionStore;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserConnectionStore;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserInstanceRoutingStore;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Redis;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
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
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IKafkaSettingsManager>(new KafkaSettingsManager(configuration));
        services.TryAddSingleton(sp => sp.GetRequiredService<IApiSettingsManager>().GetRealtimeConnectionsSettings());
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(sp.GetRequiredService<RealtimeConnectionsSettings>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.TryAddSingleton<RedisUnitOfWork>();
        services.TryAddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<RedisUnitOfWork>());
        services.TryAddSingleton<IRedisTransactionContext>(sp => sp.GetRequiredService<RedisUnitOfWork>());
        services.TryAddSingleton<IActiveConnectionsTracker, InMemoryActiveConnectionsTracker>();
        services.TryAddSingleton<IConnectionStore, RedisConnectionStore>();
        services.TryAddSingleton<IUserConnectionsStore, RedisUserConnectionsStore>();
        services.TryAddSingleton<RedisUserInstanceRoutingStore>();
        services.TryAddSingleton<IUserInstanceRoutingStore>(sp => sp.GetRequiredService<RedisUserInstanceRoutingStore>());
        services.TryAddSingleton<IRealtimeRoutingTopologyReader>(sp => sp.GetRequiredService<RedisUserInstanceRoutingStore>());
        services.TryAddSingleton<IRealtimeConnectionRegistry, RealtimeConnectionRegistry>();
        services.AddScoped<IKafkaProducerOptions<FlowChat.Core.Messaging.RealtimeService.Events.RealtimeConnectionRegisteredIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetRealtimeConnectionProducerOptions());
        services.AddScoped<IKafkaProducerOptions<FlowChat.Core.Messaging.RealtimeService.Events.RealtimeConnectionUnregisteredIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetRealtimeConnectionProducerOptions());
        services.AddScoped<IDirectEventPublisher, FlowChatSilverbackEventPublisher>();

        services.TryAddSingleton(sp => sp.GetRequiredService<IApiSettingsManager>().GetPresenceServiceSettings());
        services.AddHttpClient(PresenceInternalApiClient.HttpClientName, (sp, client) =>
        {
            var settings = sp.GetRequiredService<PresenceServiceSettings>();
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

        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }
}
