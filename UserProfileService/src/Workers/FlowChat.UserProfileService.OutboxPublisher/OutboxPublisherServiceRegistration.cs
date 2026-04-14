using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.OutboxPublisher.Configuration;
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
            .GetSection(UserProfileCreatedProducerOptions.SectionName)
            .Get<UserProfileCreatedProducerOptions>()
            ?? new UserProfileCreatedProducerOptions();
        var emailConfirmedProducerOptions = configuration
            .GetSection(UserEmailConfirmedProducerOptions.SectionName)
            .Get<UserEmailConfirmedProducerOptions>()
            ?? new UserEmailConfirmedProducerOptions();
        var emailVerificationRequestedProducerOptions = configuration
            .GetSection(UserEmailVerificationRequestedProducerOptions.SectionName)
            .Get<UserEmailVerificationRequestedProducerOptions>()
            ?? new UserEmailVerificationRequestedProducerOptions();
        var stateChangedProducerOptions = configuration
            .GetSection(UserProfileStateChangedProducerOptions.SectionName)
            .Get<UserProfileStateChangedProducerOptions>()
            ?? new UserProfileStateChangedProducerOptions();
        var outboxOptions = configuration
            .GetSection(OutboxPublisherRuntimeOptions.SectionName)
            .Get<OutboxPublisherRuntimeOptions>()
            ?? new OutboxPublisherRuntimeOptions();
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
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserEmailConfirmedIntegrationEvent>("user-email-confirmed", endpoint => endpoint
                            .ProduceTo(emailConfirmedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationRequestedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.Key)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserProfileChangedIntegrationEvent>("user-profile-state-changed", endpoint => endpoint
                            .ProduceTo(stateChangedProducerOptions.Topic)
                            .SetKafkaKey(message => message?.UserProfileId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}


