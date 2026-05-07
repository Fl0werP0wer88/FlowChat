using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.PresenceService.Infrastructure.Presence;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FlowChat.PresenceService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(InfrastructureServiceRegistration).Assembly);

        // Override connection string from ConnectionStrings section after appsettings binding
        services.PostConfigure<PresenceStatusSettingsSection>(settings =>
        {
            var cs = configuration.GetConnectionString(PresenceStatusSettingsSection.RedisConnectionStringName);
            if (!string.IsNullOrEmpty(cs))
                settings.RedisConnectionString = cs;
        });

        // Raw type singleton so existing consumers (IConnectionMultiplexer factory etc.) don't need to change
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<PresenceStatusSettingsSection>>().Value);
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(
                sp.GetRequiredService<PresenceStatusSettingsSection>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<PresenceStatusChangedIntegrationEvent, PresenceStatusChangedProducerSettingsSection>());
        services.AddScoped<IPresenceStatusStore, RedisPresenceStatusStore>();

        return services;
    }
}
