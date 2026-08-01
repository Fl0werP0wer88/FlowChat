using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;

public static class SilverbackBuilderTieredRetryPipelineExtensions
{
    public static SilverbackBuilder AddFlowChatTieredRetryConsumerPipeline<TDbContext>(
        this SilverbackBuilder builder,
        string bootstrapServers,
        IReadOnlyCollection<ITieredRetryKafkaConsumerSettingsSection> streams)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        ArgumentNullException.ThrowIfNull(streams);

        if (streams.Count == 0)
            throw new ArgumentException("At least one tiered Kafka retry stream is required.", nameof(streams));

        builder.Services.AddSingleton(new TieredKafkaRetryTopology(streams));
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<IKafkaRetryPartitionController, KafkaRetryPartitionController>();

        return builder
            .AddSingletonBrokerBehavior<DelayedRetryConsumerBehavior>()
            .AddSingletonBrokerBehavior<InvalidRetryMetadataConsumerBehavior>()
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(bootstrapServers)
                .AddFlowChatTieredRetryStreams<TDbContext>(streams));
    }

    public static SilverbackBuilder AddFlowChatTieredRetryProducerPipeline(
        this SilverbackBuilder builder,
        string bootstrapServers,
        IReadOnlyCollection<string> destinationTopics)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        ArgumentNullException.ThrowIfNull(destinationTopics);

        if (destinationTopics.Count == 0)
            throw new ArgumentException("At least one tiered Kafka retry destination topic is required.", nameof(destinationTopics));

        if (destinationTopics.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Tiered Kafka retry destination topics cannot be empty.", nameof(destinationTopics));

        if (destinationTopics.Distinct(StringComparer.Ordinal).Count() != destinationTopics.Count)
            throw new ArgumentException("Tiered Kafka retry destination topics must be unique.", nameof(destinationTopics));

        return builder.AddKafkaClients(clients =>
        {
            clients.WithBootstrapServers(bootstrapServers);

            foreach (var topic in destinationTopics)
            {
                clients.AddProducer(producer => producer
                    .Produce(topic, endpoint => endpoint
                        .ProduceTo(topic)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            }
        });
    }
}
