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
            .GetSection(ContactAddedProducerSettingsSection.SectionName)
            .Get<ContactAddedProducerSettingsSection>()
            ?? new ContactAddedProducerSettingsSection();
        var contactDeletedOptions = configuration
            .GetSection(ContactDeletedProducerSettingsSection.SectionName)
            .Get<ContactDeletedProducerSettingsSection>()
            ?? new ContactDeletedProducerSettingsSection();
        var outboxOptions = configuration
            .GetSection(OutboxPublisherRuntimeSettingsSection.SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>()
            ?? new OutboxPublisherRuntimeSettingsSection();
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
