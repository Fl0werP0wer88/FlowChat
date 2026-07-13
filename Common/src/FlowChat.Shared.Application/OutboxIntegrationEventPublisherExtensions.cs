using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public static class OutboxIntegrationEventPublisherExtensions
{
    public static Task PublishAsync<TEvent>(
        this IOutboxIntegrationEventPublisher publisher,
        TEvent payload,
        string kafkaKey,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(publisher);

        return publisher.PublishAsync(
            new IntegrationEventEnvelope<TEvent>(payload, kafkaKey),
            cancellationToken);
    }
}
