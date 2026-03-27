using FlowChat.ChatService.OutboxPublisher.Configuration;
using FlowChat.ChatService.Persistence;
using FlowChat.Messaging.Contracts.ChatService.Events;
using FlowChat.API.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.ChatService.OutboxPublisher;

public static class OutboxPublisherServiceRegistration
{
    public static IServiceCollection AddOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var outboxOptions = configuration
            .GetSection(OutboxPublisherRuntimeOptions.SectionName)
            .Get<OutboxPublisherRuntimeOptions>()
            ?? new OutboxPublisherRuntimeOptions();
        var producerOptions = configuration
            .GetSection(ChatMessageSentProducerOptions.SectionName)
            .Get<ChatMessageSentProducerOptions>()
            ?? new ChatMessageSentProducerOptions();

        services.AddOptions<OutboxPublisherRuntimeOptions>()
            .BindConfiguration(OutboxPublisherRuntimeOptions.SectionName);
        services.AddOptions<ChatMessageSentProducerOptions>()
            .BindConfiguration(ChatMessageSentProducerOptions.SectionName);

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
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(producerOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<ChatMessageSentIntegrationEvent>("chat-message-sent", endpoint => endpoint
                            .ProduceTo(producerOptions.Topic)
                            .SetKafkaKey(message => message?.ConversationId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}

