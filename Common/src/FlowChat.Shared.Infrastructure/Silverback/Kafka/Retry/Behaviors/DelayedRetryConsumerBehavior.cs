using System.Globalization;
using System.Diagnostics;
using Confluent.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Behaviors;

public sealed class DelayedRetryConsumerBehavior(
    TieredKafkaRetryTopology topology,
    TimeProvider timeProvider,
    IKafkaRetryPartitionController partitionController,
    ILogger<DelayedRetryConsumerBehavior> logger) : IConsumerBehavior
{
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler + 10;

    public async ValueTask HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        if (context.Envelope.BrokerMessageIdentifier is not KafkaOffset kafkaOffset ||
            !topology.TryGetRetryTopic(kafkaOffset.TopicPartition.Topic, out var retryTopic))
        {
            await next(context, cancellationToken);
            return;
        }

        var retryAtHeader = context.Envelope.Headers.GetValue(RetryMessageHeaders.RetryAtUtc);
        if (string.IsNullOrWhiteSpace(retryAtHeader))
        {
            MarkInvalid(
                context,
                $"Kafka retry message on topic '{retryTopic.Tier.Topic}' does not contain '{RetryMessageHeaders.RetryAtUtc}'.");
            await next(context, cancellationToken);
            return;
        }

        if (!DateTimeOffset.TryParseExact(
                retryAtHeader,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var retryAtUtc))
        {
            MarkInvalid(
                context,
                $"Kafka retry message on topic '{retryTopic.Tier.Topic}' contains an invalid '{RetryMessageHeaders.RetryAtUtc}'.");
            await next(context, cancellationToken);
            return;
        }

        var delay = retryAtUtc - timeProvider.GetUtcNow();
        if (delay <= TimeSpan.Zero)
        {
            SetActivityTags(retryTopic);
            await next(context, cancellationToken);
            return;
        }

        var topicPartition = kafkaOffset.TopicPartition;
        partitionController.Pause(context.Consumer, topicPartition);
        logger.LogInformation(
            "Paused Kafka retry partition {Topic}[{Partition}] for {Delay} until {RetryAtUtc}.",
            topicPartition.Topic,
            topicPartition.Partition.Value,
            delay,
            retryAtUtc);

        try
        {
            await Task.Delay(delay, timeProvider, cancellationToken);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested &&
                partitionController.IsAssigned(context.Consumer, topicPartition))
            {
                partitionController.Resume(context.Consumer, topicPartition);
                logger.LogInformation(
                    "Resumed Kafka retry partition {Topic}[{Partition}] for retry attempt {RetryAttempt}.",
                    topicPartition.Topic,
                    topicPartition.Partition.Value,
                    retryTopic.TierIndex + 1);
            }
        }

        SetActivityTags(retryTopic);
        await next(context, cancellationToken);
    }

    private static void SetActivityTags(RetryTopicRegistration retryTopic)
    {
        Activity.Current?.SetTag("retry.attempt", retryTopic.TierIndex + 1);
        Activity.Current?.SetTag("retry.tier", retryTopic.Tier.Delay.TotalSeconds);
        Activity.Current?.SetTag("original.topic", retryTopic.Stream.Topic);
    }

    private static void MarkInvalid(ConsumerPipelineContext context, string reason) =>
        context.Envelope.Headers.AddOrReplace(RetryMessageHeaders.InvalidRetryMetadata, reason);
}
