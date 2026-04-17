using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public sealed class FlowChatSilverbackEventPublisher : IOutboxIntegrationEventPublisher, IDirectEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublisher _publisher;
    private readonly ILogger<FlowChatSilverbackEventPublisher> _logger;

    public FlowChatSilverbackEventPublisher(
        IServiceProvider serviceProvider,
        IPublisher publisher,
        ILogger<FlowChatSilverbackEventPublisher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Publish<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(message.Key))
        {
            throw new InvalidOperationException(
                $"Integration event '{typeof(TEvent).FullName}' does not contain a Kafka key.");
        }

        return PublishAsync(new IntegrationEventEnvelope<TEvent>(message, message.Key), cancellationToken);
    }

    private async Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        var options = _serviceProvider.GetService<IKafkaProducerSettingsSection<TEvent>>();
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
