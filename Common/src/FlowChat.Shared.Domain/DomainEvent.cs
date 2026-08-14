using System.Reflection;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public abstract class DomainEventBase : IDomainEvent
{
    public int Version { get; set; } = 1;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AggregateId { get; set; }

    public UtcDateTimeOffset OccurredOnUtc { get; set; } = UtcDateTimeOffset.UtcNow;

    public string EventType { get; set; } = string.Empty;

    public string AggregateType { get; set; } = string.Empty;

    public string? TraceInfo { get; set; }

    public DomainEventBase()
    {
    }

    protected DomainEventBase(Guid aggregateId, UtcDateTimeOffset occurredOnUtc)
    {
        AggregateId = aggregateId != Guid.Empty
            ? aggregateId
            : throw new ArgumentNullException(nameof(aggregateId));
        OccurredOnUtc = occurredOnUtc;
        AggregateType = GetAggregateType(GetType()) ?? throw new InvalidOperationException("Aggregate type cannot be null.");
        EventType = GetEventType(this);
    }

    protected DomainEventBase(Guid aggregateId, string aggregateType, UtcDateTimeOffset occurredOnUtc)
    {
        AggregateId = aggregateId != Guid.Empty
            ? aggregateId
            : throw new ArgumentNullException(nameof(aggregateId));
        AggregateType = string.IsNullOrWhiteSpace(aggregateType)
            ? throw new ArgumentException("Aggregate type cannot be null or empty.", nameof(aggregateType))
            : aggregateType;
        OccurredOnUtc = occurredOnUtc;
        EventType = GetEventType(GetType(), AggregateType);
    }

    public static string GetAggregateType<TEvent>() where TEvent : IDomainEvent =>
        GetAggregateType(typeof(TEvent));

    public static string GetAggregateType(Type eventType)
    {
        var attribute = eventType.GetCustomAttribute<AggregateTypeAttribute>();
        return attribute?.AggregateType ?? string.Empty;
    }

    public static string GetEventType(IDomainEvent @event) =>
        GetEventType(@event.GetType(), @event.AggregateType);

    public static string GetEventType<TEvent>() where TEvent : IDomainEvent =>
        GetEventType(typeof(TEvent));

    public static string GetEventType(Type eventType, string? prefix = null)
    {
        prefix ??= GetAggregateType(eventType);
        return $"{prefix}.{eventType.Name}";
    }

    public static void StampVersions(IEnumerable<IDomainEvent> domainEvents, int version)
    {
        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is DomainEventBase mutableEvent)
            {
                mutableEvent.Version = version;
            }
        }
    }
}

