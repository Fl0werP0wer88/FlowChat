using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using Silverback.Configuration;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;

public static class SilverbackBuilderTieredRetryExtensions
{
    public static SilverbackBuilder AddFlowChatTieredRetry<TDbContext>(
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
}
