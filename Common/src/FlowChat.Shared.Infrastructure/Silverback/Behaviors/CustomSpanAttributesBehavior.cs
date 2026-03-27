using System.Diagnostics;
using FlowChat.Messaging.Contracts;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace FlowChat.Shared.Infrastructure.Silverback.Behaviors;

public sealed class CustomSpanAttributesBehavior : IProducerBehavior, IConsumerBehavior
{
    public const string EventNameTag = "flowchat.event.name";

    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageEnricher + 10;

    public async ValueTask HandleAsync(
        ProducerPipelineContext context,
        ProducerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        SetEventNameTag(context.Envelope.Headers);

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        SetEventNameTag(context.Envelope.Headers);

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private static void SetEventNameTag(MessageHeaderCollection headers)
    {
        var eventName = headers.GetValue(IntegrationMessageHeaders.EventType);

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            Activity.Current?.SetTag(EventNameTag, eventName);
        }
    }
}
