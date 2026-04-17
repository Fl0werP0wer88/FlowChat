using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new KafkaSettingsManager(configuration);
        var createdProducerOptions = settingsManager.GetUserProfileCreatedProducerSettingsSection();
        var emailConfirmedProducerOptions = settingsManager.GetUserEmailConfirmedProducerSettingsSection();
        var emailVerificationRequestedProducerOptions = settingsManager.GetUserEmailVerificationRequestedProducerSettingsSection();
        var stateChangedProducerOptions = settingsManager.GetUserProfileStateChangedProducerSettingsSection();
        var bootstrapServers = !string.IsNullOrWhiteSpace(createdProducerOptions.BootstrapServers)
            ? createdProducerOptions.BootstrapServers
            : stateChangedProducerOptions.BootstrapServers;

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
                        .Produce<UserProfileCreatedIntegrationEvent>("user-profile-created", endpoint => endpoint
                            .ProduceTo(createdProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserEmailConfirmedIntegrationEvent>("user-email-confirmed", endpoint => endpoint
                            .ProduceTo(emailConfirmedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationRequestedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.Key)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserProfileChangedIntegrationEvent>("user-profile-state-changed", endpoint => endpoint
                            .ProduceTo(stateChangedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}


