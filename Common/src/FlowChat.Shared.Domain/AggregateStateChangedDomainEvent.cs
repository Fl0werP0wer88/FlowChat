using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public sealed class AggregateStateChangedDomainEvent<TRoot, TSnapshot> : DomainEventBase, IAggregateStateChangedDomainEvent
    where TRoot : AggregateRootBase<TRoot>
{
    public AggregateStateChangedDomainEvent(
        Id<TRoot> aggregateId,
        string aggregateType,
        TSnapshot aggregateState)
        : base(aggregateId, aggregateType, UtcDateTimeOffset.UtcNow)
    {
        AggregateState = aggregateState;
    }

    public TSnapshot AggregateState { get; }
}

