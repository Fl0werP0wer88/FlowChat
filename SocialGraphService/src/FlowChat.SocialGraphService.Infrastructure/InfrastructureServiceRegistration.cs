using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using FlowChat.SocialGraphService.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.SocialGraphService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var infrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IKafkaSettingsManager>(new KafkaSettingsManager(configuration));
        services.AddScoped<IKafkaProducerOptions<ContactAddedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetContactAddedProducerOptions());
        services.AddScoped<IKafkaProducerOptions<ContactDeletedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetContactDeletedProducerOptions());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);

        return services;
    }
}
