using FlowChat.Application.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Mapping;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Infrastructure.Mapping;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
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
        var infrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IKafkaSettingsManager>(new KafkaSettingsManager(configuration));
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);
        services.AddScoped<IObjectMapper, AutoMapperObjectMapper>();
        services.AddScoped<IKafkaProducerOptions<UserProfileCreatedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetUserProfileCreatedProducerOptions());
        services.AddScoped<IIntegrationEventPublisher, SilverbackEventPublisher>();

        return services;
    }
}
