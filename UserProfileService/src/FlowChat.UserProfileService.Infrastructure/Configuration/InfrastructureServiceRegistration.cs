using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using FlowChat.UserProfileService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(InfrastructureServiceRegistration).Assembly);
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<UserProfileCreatedIntegrationEvent, UserProfileCreatedProducerSettingsSection>()
            .AddProducerSettings<UserEmailConfirmedIntegrationEvent, UserEmailConfirmedProducerSettingsSection>()
            .AddProducerSettings<EmailVerificationRequestIntegrationEvent, UserEmailVerificationRequestedProducerSettingsSection>()
            .AddProducerSettings<UserProfileChangedIntegrationEvent, UserProfileStateChangedProducerSettingsSection>()
            .AddProducerSettings<ProjectionIntegrationEvent<UserProfileReadModel>, UserProfileProjectionProducerSettingsSection>());
        services.AddScoped<IEmailVerificationLinkBuilder, EmailVerificationLinkBuilder>();
        services.AddScoped<IEmailVerificationTokenProtector, EmailVerificationTokenProtector>();

        return services;
    }
}
