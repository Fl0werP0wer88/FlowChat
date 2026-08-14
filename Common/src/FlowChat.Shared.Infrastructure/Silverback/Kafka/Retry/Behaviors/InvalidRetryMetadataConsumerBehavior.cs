using FlowChat.Core.Exceptions;
using Silverback.Messaging.Broker.Behaviors;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Behaviors;

public sealed class InvalidRetryMetadataConsumerBehavior : IConsumerBehavior
{
    // KafkaOffsetStoreConsumerBehavior runs at Publisher - 10; validation must happen after its scope is created
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher - 5;

    public ValueTask HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        var reason = context.Envelope.Headers.GetValue(RetryMessageHeaders.InvalidRetryMetadata);
        return string.IsNullOrWhiteSpace(reason)
            ? next(context, cancellationToken)
            : ValueTask.FromException(new NonTransientException(reason));
    }
}
