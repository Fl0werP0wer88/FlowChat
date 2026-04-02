using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Core.Messaging.AuthService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.AuthService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IWorkerSettingsManager>(new WorkerSettingsManager(configuration));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IKafkaProducerOptions<AccountRegisteredIntegrationEvent>>(sp =>
            sp.GetRequiredService<IWorkerSettingsManager>().GetAccountRegisteredProducerOptions());
        services.AddScoped<IKafkaProducerOptions<AccountConfirmedIntegrationEvent>>(sp =>
            new KafkaProducerOptionsAdapter<AccountConfirmedIntegrationEvent>(
                sp.GetRequiredService<IWorkerSettingsManager>().GetAccountRegisteredProducerOptions()));
        services.AddScoped<IKafkaProducerOptions<PhoneNumberConfirmedIntegrationEvent>>(sp =>
            new KafkaProducerOptionsAdapter<PhoneNumberConfirmedIntegrationEvent>(
                sp.GetRequiredService<IWorkerSettingsManager>().GetAccountRegisteredProducerOptions()));
        services.AddScoped<IIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();

        return services;
    }
}

