using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.Messaging.Contracts;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class SilverbackEventPublisher<TEvent, TOptions> : IIntegrationEventPublisher<TEvent>
    where TEvent : class
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
            envelope => EnrichEnvelope(envelope, key),
            cancellationToken);

        _logger.LogInformation(
            "Queued {EventType} event to Silverback producer for topic {Topic} with key {Key}.",
            typeof(TEvent).Name,
            _options.Topic,
            key);
    }

    private static void EnrichEnvelope(IOutboundEnvelope envelope, string key)
    {
        var messageId = Guid.NewGuid();
        var occurredOnUtc = DateTimeOffset.UtcNow;
        var messageType = typeof(TEvent);
        var activity = Activity.Current;

        envelope.SetKafkaKey(key);
        envelope.AddHeader(IntegrationMessageHeaders.EventId, messageId.ToString("D"));
        envelope.AddHeader(IntegrationMessageHeaders.OccurredOnUtc, occurredOnUtc.ToString("O"));
        envelope.AddHeader(IntegrationMessageHeaders.EventVersion, "1");
        envelope.AddHeader(IntegrationMessageHeaders.EventType, messageType.Name);
        envelope.AddHeader(IntegrationMessageHeaders.Source, ResolveSource(messageType));
        envelope.AddHeader(IntegrationMessageHeaders.CorrelationId, activity?.RootId ?? messageId.ToString("D"));
        envelope.AddHeader(IntegrationMessageHeaders.CausationId, activity?.ParentId ?? string.Empty);
        envelope.AddHeader(IntegrationMessageHeaders.TraceParent, activity?.Id ?? string.Empty);
    }

    private static string ResolveSource(Type eventType)
    {
        var namespaceParts = eventType.Namespace?.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (namespaceParts is null || namespaceParts.Length == 0)
        {
            return "unknown";
        }

        var contractsIndex = Array.IndexOf(namespaceParts, "Contracts");
        if (contractsIndex >= 0 && contractsIndex + 1 < namespaceParts.Length)
        {
            return ToKebabCase(namespaceParts[contractsIndex + 1]);
        }

        return ToKebabCase(namespaceParts[^1]);
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var builder = new StringBuilder(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
