using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.OutboxPublisher.Configuration;
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
            .GetSection(PresenceStatusChangedProducerOptions.SectionName)
            .Get<PresenceStatusChangedProducerOptions>()
            ?? new PresenceStatusChangedProducerOptions();
        var outboxOptions = configuration
            .GetSection(OutboxPublisherRuntimeOptions.SectionName)
            .Get<OutboxPublisherRuntimeOptions>()
            ?? new OutboxPublisherRuntimeOptions();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesBehavior>()
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
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(producerOptions.BootstrapServers)
                .AddProducer(producer => producer
                    .Produce<PresenceStatusChangedIntegrationEvent>("presence-status-changed", endpoint => endpoint
                        .ProduceTo(producerOptions.Topic)
                        .SetKafkaKey(message => message?.Key)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader()))));

        return services;
    }
}
