using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Messaging.Contracts.AuthService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var kafkaProducerSection = configuration.GetSection(UserCreatedProducerOptions.SectionName);
        if (!kafkaProducerSection.Exists())
        {
            kafkaProducerSection = configuration.GetSection(UserCreatedProducerOptions.FallbackSectionName);
        }

        var userEmailVerificationRequestedOutboxSection = configuration.GetSection(UserEmailVerificationRequestedProducerOptions.SectionName);
        if (!userEmailVerificationRequestedOutboxSection.Exists())
        {
            userEmailVerificationRequestedOutboxSection = configuration.GetSection(UserEmailVerificationRequestedProducerOptions.FallbackSectionName);
        }

        services.Configure<UserCreatedProducerOptions>(kafkaProducerSection);
        services.Configure<UserEmailVerificationRequestedProducerOptions>(userEmailVerificationRequestedOutboxSection);

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenEncoder, Base64UrlTokenEncoder>();
        services.AddScoped<IConfirmationLinkBuilder, ConfirmationLinkBuilder>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<
            IKafkaEventPublisher<UserCreatedIntegrationEvent>,
            SilverbackEventPublisher<UserCreatedIntegrationEvent, UserCreatedProducerOptions>>();
        services.AddScoped<
            IKafkaEventPublisher<EmailVerificationRequestIntegrationEvent>,
            SilverbackEventPublisher<EmailVerificationRequestIntegrationEvent, UserEmailVerificationRequestedProducerOptions>>();

        return services;
    }
}
