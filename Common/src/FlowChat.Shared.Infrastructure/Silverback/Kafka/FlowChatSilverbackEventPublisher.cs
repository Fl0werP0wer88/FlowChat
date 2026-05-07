using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public sealed class FlowChatSilverbackEventPublisher : IOutboxIntegrationEventPublisher, IDirectEventPublisher
{
    private readonly KafkaProducerSettingsRegistry _registry;
    private readonly IPublisher _publisher;
    private readonly ILogger<FlowChatSilverbackEventPublisher> _logger;

    public FlowChatSilverbackEventPublisher(
        KafkaProducerSettingsRegistry registry,
        IPublisher publisher,
        ILogger<FlowChatSilverbackEventPublisher> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);
        var options = _registry.Get<TEvent>();
        if (options is null)
        {
            throw new InvalidOperationException(
                $"Kafka producer options for event '{typeof(TEvent).FullName}' are not registered.");
        }

        if (string.IsNullOrWhiteSpace(message.KafkaKey))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        await _publisher.WrapAndPublishAsync(
            message.Payload,
            envelope => EnrichEnvelope(envelope, message),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to Silverback producer for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            options.Topic,
            message.KafkaKey);
    }

    private static void EnrichEnvelope<TEvent>(
        IOutboundEnvelope envelope,
        IntegrationEventEnvelope<TEvent> message)
        where TEvent : IntegrationEvent
    {
        envelope.SetKafkaKey(message.KafkaKey);
        foreach (var header in message.Headers)
        {
            envelope.AddHeader(header.Key, header.Value);
        }
    }
}
