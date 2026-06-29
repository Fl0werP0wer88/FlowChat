using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public static class ApiSilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var emailConfirmedProducerOptions = configuration.GetSection(new UserEmailConfirmedProducerSettingsSection().SectionName)
            .Get<UserEmailConfirmedProducerSettingsSection>() ?? new UserEmailConfirmedProducerSettingsSection();
        var emailVerificationRequestedProducerOptions = configuration.GetSection(new UserEmailVerificationRequestedProducerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedProducerSettingsSection>() ?? new UserEmailVerificationRequestedProducerSettingsSection();
        var projectionProducerOptions = configuration.GetSection(new UserProfileProjectionProducerSettingsSection().SectionName)
            .Get<UserProfileProjectionProducerSettingsSection>() ?? new UserProfileProjectionProducerSettingsSection();
        var bootstrapServers = !string.IsNullOrWhiteSpace(emailConfirmedProducerOptions.BootstrapServers)
            ? emailConfirmedProducerOptions.BootstrapServers
            : projectionProducerOptions.BootstrapServers;

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<UserEmailConfirmedIntegrationEvent>("user-email-confirmed", endpoint => endpoint
                            .ProduceTo(emailConfirmedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationRequestedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<UserProfileReadModel>>("user-profile-projection", endpoint => endpoint
                            .ProduceTo(projectionProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}


