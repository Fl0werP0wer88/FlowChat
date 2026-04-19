using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.PresenceService.Infrastructure.Presence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Configuration;
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
        services.TryAddSingleton<ISettingsProvider>(new AppSettingsProvider(configuration));
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsProvider>().GetSection<PresenceStatusSettingsSection>();
            settings.RedisConnectionString = configuration.GetConnectionString(PresenceStatusSettingsSection.RedisConnectionStringName)
                ?? settings.RedisConnectionString;
            return settings;
        });
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(
                sp.GetRequiredService<PresenceStatusSettingsSection>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddScoped<IKafkaProducerSettingsSection<PresenceStatusChangedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<PresenceStatusChangedProducerSettingsSection>());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();
        services.AddScoped<IPresenceStatusStore, RedisPresenceStatusStore>();

        return services;
    }
}
