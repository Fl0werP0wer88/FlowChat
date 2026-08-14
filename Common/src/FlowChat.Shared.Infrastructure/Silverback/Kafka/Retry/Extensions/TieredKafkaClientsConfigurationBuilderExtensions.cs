using Confluent.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;

public static class TieredKafkaClientsConfigurationBuilderExtensions
{
    public static KafkaClientsConfigurationBuilder AddFlowChatTieredRetryStreams<TDbContext>(
        this KafkaClientsConfigurationBuilder clients,
        IEnumerable<ITieredRetryKafkaConsumerSettingsSection> streams)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(streams);

        foreach (var stream in streams)
            clients.AddFlowChatTieredRetryStream<TDbContext>(stream);

        return clients;
    }

    public static KafkaClientsConfigurationBuilder AddFlowChatTieredRetryStream<TDbContext>(
        this KafkaClientsConfigurationBuilder clients,
        ITieredRetryKafkaConsumerSettingsSection settings)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(settings);

        clients.AddConsumer(consumer => consumer
            .WithGroupId(settings.GroupId)
            .WithAutoOffsetReset(ParseAutoOffsetReset(settings.AutoOffsetReset))
            .DisableOffsetsCommit()
            .StoreOffsetsClientSide(store => store.UseEntityFramework<TDbContext>())
            .Consume(endpoint => endpoint.ConfigureFlowChatTieredMainEndpoint(settings)));

        for (var tierIndex = 0; tierIndex < settings.RetryTiers.Count; tierIndex++)
        {
            var capturedTierIndex = tierIndex;
            clients.AddConsumer(consumer => consumer
                .WithGroupId(settings.RetryGroupId)
                .WithAutoOffsetReset(ParseAutoOffsetReset(settings.AutoOffsetReset))
                .DisableOffsetsCommit()
                .StoreOffsetsClientSide(store => store.UseEntityFramework<TDbContext>())
                .Consume(endpoint => endpoint.ConfigureFlowChatTieredRetryEndpoint(settings, capturedTierIndex)));
        }

        foreach (var destinationTopic in settings.RetryTiers
                     .Select(tier => tier.Topic)
                     .Append(settings.DeadLetterTopic))
        {
            clients.AddProducer(producer => producer
                .Produce(destinationTopic, endpoint => endpoint
                    .ProduceTo(destinationTopic)
                    .SerializeAsJson(serializer => serializer.SetTypeHeader())
                    .StoreToOutbox(outbox => outbox.UseEntityFramework<TDbContext>())));
        }

        return clients;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
