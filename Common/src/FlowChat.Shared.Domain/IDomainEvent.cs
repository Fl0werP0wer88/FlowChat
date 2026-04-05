using MediatR;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public interface IDomainEvent : INotification
{
    int Version { get; }

    string AggregateType { get; }

    string EventType { get; }

    Guid Id { get; }

    UtcDateTimeOffset OccurredOnUtc { get; }

    Guid AggregateId { get; }

    string? TraceInfo { get; }
}

