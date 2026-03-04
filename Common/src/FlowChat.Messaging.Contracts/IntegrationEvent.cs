using System.Text;

namespace FlowChat.Messaging.Contracts;

public abstract class IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public int Version { get; init; } = 1;
    public string EventType { get; init; }
    public string Source { get; init; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? TraceId { get; init; }

    protected IntegrationEvent()
    {
        var eventType = GetType();
        EventType = eventType.Name;
        Source = ResolveSource(eventType);
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
