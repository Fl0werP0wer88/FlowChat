using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
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

        services.TryAddSingleton<ISettingsProvider>(new AppSettingsProvider(configuration));
        services.AddScoped<IKafkaProducerSettingsSection<ContactAddedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<ContactAddedProducerSettingsSection>());
        services.AddScoped<IKafkaProducerSettingsSection<ContactDeletedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<ContactDeletedProducerSettingsSection>());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);

        return services;
    }
}
