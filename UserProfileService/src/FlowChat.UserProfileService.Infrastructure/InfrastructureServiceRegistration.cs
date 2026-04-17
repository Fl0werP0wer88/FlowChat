using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Infrastructure.Services;
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
        services.TryAddSingleton<ISettingsProvider>(new SettingsProvider(configuration));
        services.AddScoped<IKafkaProducerSettingsSection<UserProfileCreatedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<UserProfileCreatedProducerSettingsSection>());
        services.AddScoped<IKafkaProducerSettingsSection<UserEmailConfirmedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<UserEmailConfirmedProducerSettingsSection>());
        services.AddScoped<IKafkaProducerSettingsSection<EmailVerificationRequestIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<UserEmailVerificationRequestedProducerSettingsSection>());
        services.AddScoped<IKafkaProducerSettingsSection<UserProfileChangedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<UserProfileStateChangedProducerSettingsSection>());
        services.AddScoped<IEmailVerificationLinkBuilder, EmailVerificationLinkBuilder>();
        services.AddScoped<IEmailVerificationTokenProtector, EmailVerificationTokenProtector>();
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();

        return services;
    }
}
