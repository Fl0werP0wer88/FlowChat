using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SocialGraphService.Infrastructure;

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
        var infrastructureAssembly = typeof(CommonInfrastructureServiceRegistration).Assembly;

        services.AddSettingsSections(configuration, infrastructureAssembly);
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<ProjectionIntegrationEvent<ContactReadModel>, ContactProjectionProducerSettingsSection>());
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);

        return services;
    }
}
