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

public static class ApiInfrastructureServiceRegistration
{
    public static IServiceCollection AddApiInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

public static class ConsumerInfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

internal static class CommonInfrastructureServiceRegistration
{
    public static IServiceCollection AddCommonInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(CommonInfrastructureServiceRegistration).Assembly);

        // Override connection string from ConnectionStrings section after appsettings binding
        services.PostConfigure<PresenceStatusSettingsSection>(settings =>
        {
            var cs = configuration.GetConnectionString(PresenceStatusSettingsSection.RedisConnectionStringName);
            if (!string.IsNullOrEmpty(cs))
                settings.RedisConnectionString = cs;
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(
                sp.GetRequiredService<IOptions<PresenceStatusSettingsSection>>().Value.RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<PresenceStatusChangedIntegrationEvent, PresenceStatusChangedProducerSettingsSection>());
        services.AddScoped<IPresenceStatusStore, RedisPresenceStatusStore>();

        return services;
    }
}
