using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SocialGraphService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var infrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

        services.AddSettingsSections(configuration, infrastructureAssembly);
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<ContactAddedIntegrationEvent, ContactAddedProducerSettingsSection>()
            .AddProducerSettings<ContactDeletedIntegrationEvent, ContactDeletedProducerSettingsSection>());
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);

        return services;
    }
}
