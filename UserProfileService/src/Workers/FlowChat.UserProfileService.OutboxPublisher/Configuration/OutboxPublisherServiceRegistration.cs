using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;
using FlowChat.UserProfileService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
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
        var emailConfirmedProducerOptions = configuration
            .GetSection(new UserEmailConfirmedProducerSettingsSection().SectionName)
            .Get<UserEmailConfirmedProducerSettingsSection>()
            ?? new UserEmailConfirmedProducerSettingsSection();
        var emailVerificationRequestedProducerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedProducerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedProducerSettingsSection>()
            ?? new UserEmailVerificationRequestedProducerSettingsSection();
        var projectionProducerOptions = configuration
            .GetSection(new UserProfileProjectionProducerSettingsSection().SectionName)
            .Get<UserProfileProjectionProducerSettingsSection>()
            ?? new UserProfileProjectionProducerSettingsSection();
        var outboxOptions = configuration
            .GetSection(new OutboxPublisherRuntimeSettingsSection().SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>()
            ?? new OutboxPublisherRuntimeSettingsSection();
        var retryKafkaOptions = configuration
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()
            ?? new RetryOutboxKafkaSettingsSection();
        var bootstrapServers = !string.IsNullOrWhiteSpace(emailConfirmedProducerOptions.BootstrapServers)
            ? emailConfirmedProducerOptions.BootstrapServers
            : !string.IsNullOrWhiteSpace(projectionProducerOptions.BootstrapServers)
                ? projectionProducerOptions.BootstrapServers
                : retryKafkaOptions.BootstrapServers;

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(outboxOptions.BatchSize)
                    .WithInterval(outboxOptions.PollInterval)
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
                        .Produce<UserEmailConfirmedIntegrationEvent>("user-email-confirmed", endpoint => endpoint
                            .ProduceTo(emailConfirmedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationRequestedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<UserProfileReadModel>>("user-profile-projection", endpoint => endpoint
                            .ProduceTo(projectionProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddFlowChatTieredRetryProducerPipeline(
                retryKafkaOptions.BootstrapServers,
                retryKafkaOptions.Topics);

        return services;
    }
}


