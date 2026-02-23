using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Models;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Infrastructure.Services;
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

        services.Configure<UserCreatedProducerOptions>(kafkaProducerSection);

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenEncoder, Base64UrlTokenEncoder>();
        services.AddScoped<IConfirmationLinkBuilder, ConfirmationLinkBuilder>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<
            IKafkaEventPublisher<UserCreatedEvent>,
            KafkaEventPublisher<UserCreatedEvent, UserCreatedProducerOptions>>();

        return services;
    }
}
