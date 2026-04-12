using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Infrastructure.Configuration;
using FlowChat.PresenceService.Infrastructure.Kafka;
using FlowChat.PresenceService.Infrastructure.Presence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FlowChat.PresenceService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IKafkaSettingsManager>(new KafkaSettingsManager(configuration));
        services.TryAddSingleton(sp => sp.GetRequiredService<IApiSettingsManager>().GetPresenceStatusSettings());
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(
                sp.GetRequiredService<PresenceStatusSettings>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddScoped<IKafkaProducerOptions<PresenceStatusChangedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetPresenceStatusChangedProducerOptions());
        services.AddScoped<IIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();
        services.AddScoped<IPresenceStatusStore, RedisPresenceStatusStore>();
        services.AddScoped<IPresenceStatusUpdateService, PresenceStatusUpdateService>();

        return services;
    }
}
