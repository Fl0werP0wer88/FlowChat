using System.Diagnostics;
using System.Text;

namespace FlowChat.Messaging.Contracts;

public class IntegrationEventEnvelope<TEvent> where TEvent : IntegrationEvent
{
    public string? KafkaKey { get; private set; }
    public TEvent Payload { get; }
    public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();


    public IntegrationEventEnvelope(TEvent payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var messageId = Guid.NewGuid();
        var occurredOnUtc = DateTimeOffset.UtcNow;
        var messageType = typeof(TEvent);
        var activity = Activity.Current;

        Payload = payload;
        Headers.Add(IntegrationMessageHeaders.EventId, messageId.ToString("D"));
        Headers.Add(IntegrationMessageHeaders.OccurredOnUtc, occurredOnUtc.ToString("O"));
        Headers.Add(IntegrationMessageHeaders.EventVersion, "1");
        Headers.Add(IntegrationMessageHeaders.EventType, messageType.Name);
        Headers.Add(IntegrationMessageHeaders.Source, ResolveSource(messageType));
        Headers.Add(IntegrationMessageHeaders.CorrelationId, activity?.RootId ?? messageId.ToString("D"));
        Headers.Add(IntegrationMessageHeaders.CausationId, activity?.ParentId ?? string.Empty);
        Headers.Add(IntegrationMessageHeaders.TraceParent, activity?.Id ?? string.Empty);
    }

    public void SetKafkaKey(string kafkaKey)
    {
        if (string.IsNullOrWhiteSpace(kafkaKey))
        {
            throw new ArgumentException("Kafka key cannot be null or empty.", nameof(kafkaKey));
        }

        KafkaKey = kafkaKey;
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
