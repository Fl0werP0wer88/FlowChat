using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Core.Messaging.AuthService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.AuthService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IWorkerSettingsManager>(new WorkerSettingsManager(configuration));

        services.AddScoped<ITokenEncoder, Base64UrlTokenEncoder>();
        services.AddScoped<IConfirmationLinkBuilder, ConfirmationLinkBuilder>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IKafkaProducerOptions<UserCreatedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IWorkerSettingsManager>().GetUserCreatedProducerOptions());
        services.AddScoped<IKafkaProducerOptions<UserConfirmedIntegrationEvent>>(sp =>
            new KafkaProducerOptionsAdapter<UserConfirmedIntegrationEvent>(
                sp.GetRequiredService<IWorkerSettingsManager>().GetUserCreatedProducerOptions()));
        services.AddScoped<IKafkaProducerOptions<EmailConfirmedIntegrationEvent>>(sp =>
            new KafkaProducerOptionsAdapter<EmailConfirmedIntegrationEvent>(
                sp.GetRequiredService<IWorkerSettingsManager>().GetUserCreatedProducerOptions()));
        services.AddScoped<IKafkaProducerOptions<PhoneNumberConfirmedIntegrationEvent>>(sp =>
            new KafkaProducerOptionsAdapter<PhoneNumberConfirmedIntegrationEvent>(
                sp.GetRequiredService<IWorkerSettingsManager>().GetUserCreatedProducerOptions()));
        services.AddScoped<IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent>>(sp =>
            sp.GetRequiredService<IWorkerSettingsManager>().GetUserEmailVerificationRequestedProducerOptions());
        services.AddScoped<IIntegrationEventPublisher, SilverbackEventPublisher>();

        return services;
    }
}
