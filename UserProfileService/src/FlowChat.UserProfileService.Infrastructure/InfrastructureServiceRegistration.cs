using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.Core.Messaging.UserProfileService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.UserProfileService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IKafkaSettingsManager>(new KafkaSettingsManager(configuration));
        services.AddScoped<IKafkaProducerOptions<UserProfileCreatedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetUserProfileCreatedProducerOptions());
        services.AddScoped<IKafkaProducerOptions<UserProfileStateChangedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetUserProfileStateChangedProducerOptions());
        services.AddScoped<IIntegrationEventPublisher, SilverbackEventPublisher>();

        return services;
    }
}

