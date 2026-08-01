using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.OutboxPublisher.Configuration.Settings;
using FlowChat.PresenceService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.PresenceService.OutboxPublisher;

public static class OutboxPublisherServiceRegistration
{
    public static IServiceCollection AddOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var producerOptions = configuration
            .GetSection(new PresenceStatusChangedProducerSettingsSection().SectionName)
            .Get<PresenceStatusChangedProducerSettingsSection>()
            ?? new PresenceStatusChangedProducerSettingsSection();
        var outboxOptions = configuration
            .GetSection(new OutboxPublisherRuntimeSettingsSection().SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>()
            ?? new OutboxPublisherRuntimeSettingsSection();

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
                        outboxOptions.InitialRetryDelay,
                        2,
                        outboxOptions.MaxRetryDelay)
                    .WithoutDistributedLock());
            })
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(producerOptions.BootstrapServers)
                .AddProducer(producer => producer
                    .Produce<PresenceStatusChangedIntegrationEvent>("presence-status-changed", endpoint => endpoint
                        .ProduceTo(producerOptions.Topic)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader()))));

        return services;
    }
}
