using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;
using FlowChat.UserProfileService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.UserProfileService.OutboxPublisher;

public static class OutboxPublisherServiceRegistration
{
    public static IServiceCollection AddOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var createdProducerOptions = configuration
            .GetSection(new UserProfileCreatedProducerSettingsSection().SectionName)
            .Get<UserProfileCreatedProducerSettingsSection>()
            ?? new UserProfileCreatedProducerSettingsSection();
        var emailConfirmedProducerOptions = configuration
            .GetSection(new UserEmailConfirmedProducerSettingsSection().SectionName)
            .Get<UserEmailConfirmedProducerSettingsSection>()
            ?? new UserEmailConfirmedProducerSettingsSection();
        var emailVerificationRequestedProducerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedProducerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedProducerSettingsSection>()
            ?? new UserEmailVerificationRequestedProducerSettingsSection();
        var stateChangedProducerOptions = configuration
            .GetSection(new UserProfileStateChangedProducerSettingsSection().SectionName)
            .Get<UserProfileStateChangedProducerSettingsSection>()
            ?? new UserProfileStateChangedProducerSettingsSection();
        var outboxOptions = configuration
            .GetSection(new OutboxPublisherRuntimeSettingsSection().SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>()
            ?? new OutboxPublisherRuntimeSettingsSection();
        var bootstrapServers = !string.IsNullOrWhiteSpace(createdProducerOptions.BootstrapServers)
            ? createdProducerOptions.BootstrapServers
            : stateChangedProducerOptions.BootstrapServers;

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(outboxOptions.BatchSize)
                    .WithInterval(TimeSpan.FromSeconds(outboxOptions.PollIntervalSeconds))
                    .WithExponentialRetryDelay(
                        TimeSpan.FromSeconds(outboxOptions.RetryBaseDelaySeconds),
                        2,
                        TimeSpan.FromSeconds(outboxOptions.MaxRetryDelaySeconds))
                    .WithoutDistributedLock());
            })
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<UserProfileCreatedIntegrationEvent>("user-profile-created", endpoint => endpoint
                            .ProduceTo(createdProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserEmailConfirmedIntegrationEvent>("user-email-confirmed", endpoint => endpoint
                            .ProduceTo(emailConfirmedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationRequestedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserProfileChangedIntegrationEvent>("user-profile-state-changed", endpoint => endpoint
                            .ProduceTo(stateChangedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}


