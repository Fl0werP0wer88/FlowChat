using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class SilverbackEventPublisher<TEvent, TOptions> : IIntegrationEventPublisher<TEvent>
    where TEvent : IntegrationEvent
    where TOptions : class, IKafkaProducerOptions<TEvent>
{
    private readonly TOptions _options;
    private readonly IPublisher _publisher;
    private readonly ILogger<SilverbackEventPublisher<TEvent, TOptions>> _logger;

    public SilverbackEventPublisher(
        IOptions<TOptions> options,
        IPublisher publisher,
        ILogger<SilverbackEventPublisher<TEvent, TOptions>> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(TEvent message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var key = _options.KeySelector(message);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Kafka message key cannot be null or empty.");
        }

        await _publisher.WrapAndPublishAsync(
            message,
            envelope => envelope.SetKafkaKey(key),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to Silverback producer for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            _options.Topic,
            key);
    }
}
