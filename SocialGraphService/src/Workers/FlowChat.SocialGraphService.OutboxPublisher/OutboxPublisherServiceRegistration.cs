using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.SocialGraphService.OutboxPublisher.Configuration;
using FlowChat.SocialGraphService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.SocialGraphService.OutboxPublisher;

public static class OutboxPublisherServiceRegistration
{
    public static IServiceCollection AddOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var contactAddedOptions = configuration
            .GetSection(ContactAddedProducerOptions.SectionName)
            .Get<ContactAddedProducerOptions>()
            ?? new ContactAddedProducerOptions();
        var contactDeletedOptions = configuration
            .GetSection(ContactDeletedProducerOptions.SectionName)
            .Get<ContactDeletedProducerOptions>()
            ?? new ContactDeletedProducerOptions();
        var outboxOptions = configuration
            .GetSection(OutboxPublisherRuntimeOptions.SectionName)
            .Get<OutboxPublisherRuntimeOptions>()
            ?? new OutboxPublisherRuntimeOptions();
        var bootstrapServers = !string.IsNullOrWhiteSpace(contactAddedOptions.BootstrapServers)
            ? contactAddedOptions.BootstrapServers
            : contactDeletedOptions.BootstrapServers;

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
                        .Produce<ContactAddedIntegrationEvent>("social-graph-contact-added", endpoint => endpoint
                            .ProduceTo(contactAddedOptions.Topic)
                            .SetKafkaKey(message => message?.Key)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<ContactDeletedIntegrationEvent>("social-graph-contact-deleted", endpoint => endpoint
                            .ProduceTo(contactDeletedOptions.Topic)
                            .SetKafkaKey(message => message?.Key)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}
